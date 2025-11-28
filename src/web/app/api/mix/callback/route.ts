import { NextRequest, NextResponse } from "next/server";
import { MixTask } from "../../../../entities/mix-task/model/mix-task.model";
import { generatePresignedDownloadUrl } from "../../../../shared/lib/minio";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";

export async function POST(req: NextRequest) {
    try {
        const { task_id, status, output_file, error_message } =
            await req.json();

        const mixTask = await MixTask.findById(task_id);
        if (!mixTask) {
            return NextResponse.json(
                { error: "Task not found" },
                { status: 404 }
            );
        }

        if (status === "COMPLETED" && output_file) {
            mixTask.status = "completed";
            mixTask.completedAt = new Date();

            // Генерируем presigned URL для скачивания
            const downloadUrl = await generatePresignedDownloadUrl(output_file);

            mixTask.outputFile = {
                minioObjectName: output_file,
                downloadUrl,
            };
        } else if (status === "FAILED") {
            mixTask.status = "failed";
            mixTask.error = error_message || "Unknown error";
        }

        await mixTask.save();

        return NextResponse.json({ success: true });
    } catch (error: any) {
        console.error("Error processing callback:", error);
        return NextResponse.json({ error: error.message }, { status: 500 });
    }
}
