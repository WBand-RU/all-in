import { NextRequest, NextResponse } from "next/server"
import { v4 as uuidv4 } from 'uuid'
import { MixTask } from '../../../../entities/mix-task/model/mix-task.model'
import { generatePresignedUploadUrl } from '../../../../shared/lib/minio'
import { ensureDatabaseConnection } from '../../../../shared/lib/db-health'

export const runtime = 'nodejs'
export const dynamic = 'force-dynamic'

export async function POST(req: NextRequest) {
  try {
    await ensureDatabaseConnection()
    
    const userId = req.headers.get('x-user-id') || 'anonymous'

    const body = await req.json()
    const { fileNames, format, normalize, targetLufs, outputName } = body

    if (!fileNames || !Array.isArray(fileNames) || fileNames.length === 0) {
      return NextResponse.json({ error: 'No file names provided' }, { status: 400 })
    }

    const taskId = uuidv4()
    
    // Создаем presigned URLs для загрузки каждого файла
    const filesWithUrls = await Promise.all(
      fileNames.map(async (fileName: string) => {
        const objectName = `mix-tasks/${taskId}/${uuidv4()}-${fileName}`
        const uploadUrl = await generatePresignedUploadUrl(objectName)
        
        return {
          originalName: fileName,
          minioObjectName: objectName,
          uploadUrl,
          uploaded: false
        }
      })
    )

    // Создаем задачу в БД
    const mixTask = new MixTask({
      userId,
      status: 'uploading',
      files: filesWithUrls,
      settings: {
        format: format || 'wav',
        normalize: normalize || false,
        targetLufs: targetLufs || -14,
        outputName: outputName || `mix.${format || 'wav'}`
      }
    })

    await mixTask.save()

    return NextResponse.json({
      taskId: mixTask._id,
      files: filesWithUrls.map(f => ({
        originalName: f.originalName,
        uploadUrl: f.uploadUrl
      }))
    })

  } catch (error: any) {
    console.error('Error creating mix task:', error)
    
    if (error.name === 'MongoTimeoutError' || error.message?.includes('timed out')) {
      return NextResponse.json({ 
        error: 'Database connection timeout. Please try again.' 
      }, { status: 503 })
    }
    
    if (error.name === 'MongoNetworkError') {
      return NextResponse.json({ 
        error: 'Database connection failed. Please check your connection.' 
      }, { status: 503 })
    }
    
    return NextResponse.json({ 
      error: 'Internal server error. Please try again later.' 
    }, { status: 500 })
  }
}
