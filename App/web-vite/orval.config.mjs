import { defineConfig } from "orval";

export default defineConfig({
    songApi: {
        input: {
            target: process.env.WEBAPI_OPENAPI ?? "http://localhost:5000/openapi/v1.json",
            filters: { tags: ["Songs", "SongSections", "SongHistory"] },
        },
        output: {
            mode: "single",
            target: "./src/lib/generated-api/song-api/songs.ts",
            schemas: "./src/lib/generated-api/song-api/models",
            client: "react-query",
            httpClient: "axios",
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
