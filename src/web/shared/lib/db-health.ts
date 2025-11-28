import mongoose from "mongoose";
import { dbConnect } from "./mongodb";

export async function checkDatabaseHealth(): Promise<{
    isConnected: boolean;
    error?: string;
    latency?: number;
}> {
    try {
        const startTime = Date.now();
        await dbConnect();

        const db = mongoose.connection.db;
        if (!db) {
            throw new Error("Database connection not established");
        }

        const pingResult = await db.admin().ping();
        const latency = Date.now() - startTime;

        if (pingResult.ok === 1) {
            return {
                isConnected: true,
                latency,
            };
        } else {
            return {
                isConnected: false,
                error: "Database ping failed",
            };
        }
    } catch (error: any) {
        return {
            isConnected: false,
            error: error.message,
        };
    }
}

export async function ensureDatabaseConnection(): Promise<void> {
    const health = await checkDatabaseHealth();

    if (!health.isConnected) {
        throw new Error(`Database connection failed: ${health.error}`);
    }

    if (health.latency && health.latency > 5000) {
        console.warn(`Database connection is slow: ${health.latency}ms`);
    }
}
