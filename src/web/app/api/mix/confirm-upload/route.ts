import { NextRequest, NextResponse } from "next/server";
import { IMixTask, MixTask } from "@/entities/mix-task/model/mix-task.model";
import { addToQueue } from "@/shared/lib/redis";
import { IMixFile } from "@/entities/mix-task/model/mix-task.model";
import { dbConnect } from "@/shared/lib/mongodb";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";

export async function POST(req: NextRequest) {
    try {
        const userId = req.headers.get("x-user-id") || "anonymous";

        const { taskId, fileName } = await req.json();

        await dbConnect();
        const mixTask = (await MixTask.findById(taskId)) as IMixTask | null;
        if (!mixTask || mixTask.userId !== userId) {
            return NextResponse.json(
                { error: "Task not found" },
                { status: 404 }
            );
        }

        // Отмечаем файл как загруженный
        const fileIndex = mixTask.files.findIndex(
            (f: IMixFile) => f.originalName === fileName
        );
        if (fileIndex === -1) {
            return NextResponse.json(
                { error: "File not found in task" },
                { status: 404 }
            );
        }

        mixTask.files[fileIndex].uploaded = true;
        mixTask.files[fileIndex].uploadedAt = new Date();

        // Проверяем, все ли файлы загружены
        const allUploaded = mixTask.files.every((f: IMixFile) => f.uploaded);

        if (allUploaded) {
            mixTask.status = "processing";

            // Отправляем задачу в очередь микшера
            const taskData = {
                task_id: mixTask._id.toString(),
                input_files: mixTask.files.map((f) => f.minioObjectName),
                output_file: `mix-results/${mixTask._id}/${mixTask.settings.outputName}`,
                format: mixTask.settings.format,
                normalize: mixTask.settings.normalize,
                target_lufs: mixTask.settings.targetLufs,
                callback_url: `${
                    process.env.APP_BASE_URL || req.nextUrl.origin
                }/api/mix/callback`,
            };

            await addToQueue("mix_tasks", taskData);
        }

        await mixTask.save();

        return NextResponse.json({
            success: true,
            allUploaded,
            status: mixTask.status,
        });
    } catch (error: any) {
        console.error("Error confirming upload:", error);
        return NextResponse.json({ error: error.message }, { status: 500 });
    }
}
