import { NextRequest, NextResponse } from "next/server";
import { MixTask } from "../../../../../entities/mix-task/model/mix-task.model";
import { dbConnect } from "@/shared/lib/mongodb";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";

export async function GET(
    req: NextRequest,
    { params }: { params: { taskId: string } }
) {
    try {
        const userId = req.headers.get("x-user-id") || "anonymous";

        const { taskId } = await params;

        await dbConnect();
        const mixTask = await MixTask.findById(taskId);
        if (!mixTask || mixTask.userId !== userId) {
            return NextResponse.json(
                { error: "Task not found" },
                { status: 404 }
            );
        }

        const response = {
            taskId: mixTask._id,
            status: mixTask.status,
            files: mixTask.files.map((f: any) => ({
                originalName: f.originalName,
                uploaded: f.uploaded,
                uploadedAt: f.uploadedAt,
            })),
            settings: mixTask.settings,
            outputFile: mixTask.outputFile,
            error: mixTask.error,
            createdAt: mixTask.createdAt,
            completedAt: mixTask.completedAt,
        };

        return NextResponse.json(response);
    } catch (error: any) {
        console.error("Error getting task status:", error);
        return NextResponse.json({ error: error.message }, { status: 500 });
    }
}
