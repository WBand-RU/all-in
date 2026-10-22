"""Exercise CI routing without changing the repository's index or commits."""

import os
from pathlib import Path
import subprocess
import tempfile
import unittest


SCRIPT = Path(__file__).with_name("detect-affected.sh").resolve()


class DetectAffectedTests(unittest.TestCase):
    def detect(self, files, base="base"):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            git = root / "git"
            git.write_text('#!/bin/bash\nif [[ "$1" != "cat-file" ]]; then printf "%s\\n" "$CHANGED_FILES"; fi\n')
            git.chmod(0o755)
            output = root / "output"
            env = {
                **os.environ,
                "PATH": f"{root}:{os.environ['PATH']}",
                "CHANGED_FILES": "\n".join(files),
                "GITHUB_OUTPUT": str(output),
                "GITHUB_STEP_SUMMARY": str(root / "summary"),
            }
            subprocess.run(["bash", str(SCRIPT), base, "head"], env=env, check=True)
            return dict(line.split("=", 1) for line in output.read_text().splitlines())

    def test_active_api_tests_trigger_backend_gate(self):
        self.assertEqual(self.detect(["Tests/WBand.WebAPI.Tests/EventsApiTests.cs"])["backend"], "true")

    def test_proxy_changes_trigger_backend_gate(self):
        self.assertEqual(self.detect(["App/WBand.WebProxy/SessionTokenTransform.cs"])["backend"], "true")

    def test_event_module_changes_trigger_api_and_backend(self):
        result = self.detect(["Modules/WBand.Modules.EventModule/EventModule.cs"])
        self.assertEqual((result["webapi"], result["backend"]), ("true", "true"))

    def test_web_changes_only_trigger_web(self):
        self.assertEqual(self.detect(["App/web/app/page.tsx"]), {
            "webapi": "false", "proxy": "false", "web": "true", "backend": "false",
        })

    def test_documentation_does_not_trigger_builds(self):
        self.assertTrue(all(value == "false" for value in self.detect(["docs/infra.md"]).values()))

    def test_workflow_changes_trigger_all_checks(self):
        self.assertTrue(all(value == "true" for value in self.detect([".github/workflows/ci-cd.yml"]).values()))

    def test_proxy_changes_trigger_proxy_image_and_backend(self):
        result = self.detect(["App/WBand.WebProxy/Program.cs"])
        self.assertEqual((result["proxy"], result["backend"], result["webapi"]), ("true", "true", "false"))

    def test_auth_tests_trigger_backend_gate(self):
        self.assertEqual(self.detect(["Tests/WBand.Authentication.Tests/ProxyRoutingTests.cs"])["backend"], "true")

    def test_legacy_projects_do_not_trigger_mvp(self):
        files = ["App/WBand.MixerWorker/Program.cs", "Modules/StemModule/Program.cs",
                 "Services/Users/User.cs", "src/App.tsx", "App/web-vite/src/App.tsx"]
        for file in files:
            with self.subTest(file=file):
                self.assertTrue(all(value == "false" for value in self.detect([file]).values()))

    def test_first_push_scans_all_files(self):
        self.assertEqual(self.detect(["App/web/app/page.tsx"], base="")["web"], "true")


if __name__ == "__main__":
    unittest.main()
