import mongoose, { Schema, Document } from "mongoose";

export interface IMixFile {
    originalName: string;
    minioObjectName: string;
    uploadUrl?: string;
    uploaded: boolean;
    uploadedAt?: Date;
}

export interface IMixTask extends Document {
    _id: string;
    userId: string;
    status: "pending" | "uploading" | "processing" | "completed" | "failed";
    files: IMixFile[];
    outputFile?: {
        minioObjectName: string;
        downloadUrl?: string;
    };
    settings: {
        format: string;
        normalize: boolean;
        targetLufs: number;
        outputName: string;
    };
    error?: string;
    createdAt: Date;
    updatedAt: Date;
    completedAt?: Date;
}

const mixFileSchema = new Schema<IMixFile>({
    originalName: { type: String, required: true },
    minioObjectName: { type: String, required: true },
    uploadUrl: { type: String },
    uploaded: { type: Boolean, default: false },
    uploadedAt: { type: Date },
});

const mixTaskSchema = new Schema<IMixTask>(
    {
        userId: {
            type: String,
            required: true,
            index: true,
        },
        status: {
            type: String,
            enum: ["pending", "uploading", "processing", "completed", "failed"],
            default: "pending",
            index: true,
        },
        files: [mixFileSchema],
        outputFile: {
            minioObjectName: String,
            downloadUrl: String,
        },
        settings: {
            format: { type: String, required: true },
            normalize: { type: Boolean, default: false },
            targetLufs: { type: Number, default: -14 },
            outputName: { type: String, required: true },
        },
        error: String,
        completedAt: Date,
    },
    {
        timestamps: true,
    }
);

export const MixTask =
    mongoose.models.MixTask ||
    mongoose.model<IMixTask>("MixTask", mixTaskSchema);
