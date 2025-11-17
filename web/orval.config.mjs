import { defineConfig } from "orval";

export default defineConfig({
    bandApi: {
        input: {
            target: "http://localhost/band-api/openapi/v1.json",
            validation: false,
        },
        output: {
            mode: "tags-split",
            target: "./src/lib/generated-api/band-api",
            schemas: "./src/lib/generated-api/band-api/models",
            client: "react-query",
            mock: false,
            baseUrl: "/band-api",
            clean: true,
            override: {
                mutator: {
                    path: "./src/lib/axios-instance.ts",
                    name: "customInstance",
                },
            },
        },
    },
    songApi: {
        input: {
            target: "http://localhost/song-api/openapi/v1.json",
            validation: false,
        },
        output: {
            mode: "tags-split",
            target: "./src/lib/generated-api/song-api",
            schemas: "./src/lib/generated-api/song-api/models",
            client: "react-query",
            mock: false,
            baseUrl: "/song-api",
            clean: true,
            override: {
                mutator: {
                    path: "./src/lib/axios-instance.ts",
                    name: "customInstance",
                },
            },
        },
    },
});
