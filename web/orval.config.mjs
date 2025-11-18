import { defineConfig } from "orval";

function createApiConfig(apiName) {
    return {
        input: {
            target: `http://localhost/${apiName}/openapi/v1.json`,
            validation: false,
        },
        output: {
            mode: "tags",
            target: `./src/lib/generated-api/${apiName}`,
            schemas: `./src/lib/generated-api/${apiName}/models`,
            client: "react-query",
            mock: false,
            baseUrl: `/${apiName}`,
            clean: true,
            override: {
                mutator: {
                    path: "./src/lib/axios-instance.ts",
                    name: "customInstance",
                },
            },
        },
    };
}

export default defineConfig({
    bandApi: createApiConfig("band-api"),
    songApi: createApiConfig("song-api"),
});
