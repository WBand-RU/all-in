import type { NextConfig } from "next";

const nextConfig: NextConfig = {
    async rewrites() {
        const origin = (
            process.env.BFF_ORIGIN ?? "http://localhost:4000"
        ).replace(/\/$/, "");
        return ["auth", "api", "band-api", "song-api"].map((prefix) => ({
            source: `/${prefix}/:path*`,
            destination: `${origin}/${prefix}/:path*`,
        }));
    },
};

export default nextConfig;
