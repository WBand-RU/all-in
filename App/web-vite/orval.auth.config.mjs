import { defineConfig } from "orval";

export default defineConfig({
    auth: {
        input: { target: "http://localhost:4000/openapi/v1.json" },
        output: {
            target: "./src/lib/generated-api/auth/auth.ts",
            schemas: "./src/lib/generated-api/auth/models",
            client: "fetch",
            clean: true,
        },
    },
});
