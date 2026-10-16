import { useNavigate, useLocation } from "react-router";

export function useNavigator() {
    const navigate = useNavigate();
    const location = useLocation();

    const go = (path: string) => {
        if (path.startsWith("/")) {
            // Absolute path - prepend with /app if not already
            if (path.startsWith("/app")) {
                navigate(path);
            } else {
                navigate(`/app${path}`);
            }
        } else if (path.startsWith("..")) {
            // Handle multiple parent levels (.., ../.., ../../../, etc.)
            const currentPath = location.pathname;
            const pathSegments = currentPath.split("/").filter(Boolean);
            const parts = path.split("/");

            let levelsUp = 0;
            for (const part of parts) {
                if (part === "..") {
                    levelsUp++;
                } else {
                    // If there's a path after the .. parts, handle it
                    break;
                }
            }

            // Remove the specified number of segments from the end
            const remainingSegments = pathSegments.slice(
                0,
                Math.max(0, pathSegments.length - levelsUp),
            );
            let newPath = `/${remainingSegments.join("/")}`;

            // If there are remaining parts after the .., append them
            const remainingPath = parts
                .slice(levelsUp)
                .filter((p) => p && p !== "..")
                .join("/");
            if (remainingPath) {
                newPath = newPath
                    ? `${newPath}/${remainingPath}`
                    : `/${remainingPath}`;
            }

            // Ensure we have at least /app
            if (!newPath || newPath === "/") {
                newPath = "/app";
            }

            navigate(newPath);
        } else {
            // Relative path - append to current path
            const currentPath = location.pathname;
            const newPath = currentPath.endsWith("/")
                ? `${currentPath}${path}`
                : `${currentPath}/${path}`;
            navigate(newPath);
        }
    };

    return { go };
}
