import { Client } from 'minio'

const minioClient = new Client({
  endPoint: process.env.MINIO_ENDPOINT || 'localhost',
  port: parseInt(process.env.MINIO_PORT || '9000'),
  useSSL: process.env.MINIO_USE_SSL === 'true',
  accessKey: process.env.MINIO_ACCESS_KEY || 'minioadmin',
  secretKey: process.env.MINIO_SECRET_KEY || 'minioadmin123',
})

const BUCKET_NAME = process.env.MINIO_BUCKET || 'wband-files'

export const ensureBucket = async () => {
  const exists = await minioClient.bucketExists(BUCKET_NAME)
  if (!exists) {
    await minioClient.makeBucket(BUCKET_NAME)
  }
}

export const generatePresignedUploadUrl = async (objectName: string): Promise<string> => {
  await ensureBucket()
  return await minioClient.presignedPutObject(BUCKET_NAME, objectName, 3600)
}

export const generatePresignedDownloadUrl = async (objectName: string): Promise<string> => {
  await ensureBucket()
  return await minioClient.presignedGetObject(BUCKET_NAME, objectName, 3600)
}

export { minioClient, BUCKET_NAME }
