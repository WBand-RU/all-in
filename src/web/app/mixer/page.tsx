'use client'

import { useState, useRef, useEffect } from 'react'

const formats = ['wav', 'mp3', 'flac', 'ogg', 'aac'] as const

type FileWithProgress = {
  file: File
  progress: number
  uploaded: boolean
  error?: string
}

type TaskStatus = 'pending' | 'uploading' | 'processing' | 'completed' | 'failed'

export default function MixerPage() {
  const [files, setFiles] = useState<FileWithProgress[]>([])
  const [format, setFormat] = useState<typeof formats[number]>('wav')
  const [normalize, setNormalize] = useState(false)
  const [lufs, setLufs] = useState(-14)
  const [outName, setOutName] = useState('mix.wav')
  const [taskId, setTaskId] = useState<string | null>(null)
  const [taskStatus, setTaskStatus] = useState<TaskStatus>('pending')
  const [downloadUrl, setDownloadUrl] = useState<string | null>(null)
  const [status, setStatus] = useState('Готово')
  const fileInputRef = useRef<HTMLInputElement>(null)
  const pollInterval = useRef<NodeJS.Timeout | null>(null)

  const onAddFiles = (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files
    if (!f) return
    const list = Array.from(f).map(file => ({ file, progress: 0, uploaded: false }))
    const names = new Set(files.map(x => x.file.name + x.file.size))
    const merged = [...files]
    for (const x of list) {
      if (!names.has(x.file.name + x.file.size)) merged.push(x)
    }
    setFiles(merged)
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  const onRemove = (idx: number) => setFiles(prev => prev.filter((_, i) => i !== idx))

  const syncOutNameExt = (fmt: string) => {
    if (!outName.toLowerCase().endsWith('.' + fmt)) {
      const dot = outName.lastIndexOf('.')
      setOutName((dot > 0 ? outName.slice(0, dot) : outName) + '.' + fmt)
    }
  }

  const onFormatChange = (v: string) => {
    const fmt = v as typeof formats[number]
    setFormat(fmt)
    syncOutNameExt(fmt)
  }

  const uploadFileToMinio = async (file: File, uploadUrl: string, index: number): Promise<boolean> => {
    try {
      const xhr = new XMLHttpRequest()
      
      return new Promise((resolve, reject) => {
        xhr.upload.addEventListener('progress', (e) => {
          if (e.lengthComputable) {
            const progress = Math.round((e.loaded / e.total) * 100)
            setFiles(prev => prev.map((f, i) => 
              i === index ? { ...f, progress } : f
            ))
          }
        })

        xhr.addEventListener('load', () => {
          if (xhr.status >= 200 && xhr.status < 300) {
            setFiles(prev => prev.map((f, i) => 
              i === index ? { ...f, uploaded: true, progress: 100 } : f
            ))
            resolve(true)
          } else {
            setFiles(prev => prev.map((f, i) => 
              i === index ? { ...f, error: `Upload failed: ${xhr.statusText}` } : f
            ))
            reject(new Error(`Upload failed: ${xhr.statusText}`))
          }
        })

        xhr.addEventListener('error', () => {
          setFiles(prev => prev.map((f, i) => 
            i === index ? { ...f, error: 'Upload failed' } : f
          ))
          reject(new Error('Upload failed'))
        })

        xhr.open('PUT', uploadUrl)
        xhr.send(file)
      })
    } catch (error) {
      setFiles(prev => prev.map((f, i) => 
        i === index ? { ...f, error: String(error) } : f
      ))
      return false
    }
  }

  const pollTaskStatus = async (taskId: string) => {
    try {
      const res = await fetch(`/api/mix/status/${taskId}`)
      if (!res.ok) return

      const data = await res.json()
      setTaskStatus(data.status)

      if (data.status === 'completed' && data.outputFile?.downloadUrl) {
        setDownloadUrl(data.outputFile.downloadUrl)
        setStatus('Готово! Можете скачать результат')
        if (pollInterval.current) {
          clearInterval(pollInterval.current)
          pollInterval.current = null
        }
      } else if (data.status === 'failed') {
        setStatus(`Ошибка: ${data.error || 'Unknown error'}`)
        if (pollInterval.current) {
          clearInterval(pollInterval.current)
          pollInterval.current = null
        }
      } else if (data.status === 'processing') {
        setStatus('Обработка...')
      }
    } catch (error) {
      console.error('Error polling status:', error)
    }
  }

  const onMix = async () => {
    if (files.length === 0) return
    
    try {
      setStatus('Создание задачи...')
      setTaskStatus('pending')
      setDownloadUrl(null)
      
      // Создаем задачу и получаем presigned URLs
      const createRes = await fetch('/api/mix/create', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          fileNames: files.map(f => f.file.name),
          format,
          normalize,
          targetLufs: lufs,
          outputName: outName
        })
      })

      if (!createRes.ok) throw new Error(await createRes.text())
      
      const { taskId: newTaskId, files: uploadUrls } = await createRes.json()
      setTaskId(newTaskId)
      setTaskStatus('uploading')
      setStatus('Загрузка файлов...')

      // Сбрасываем прогресс всех файлов
      setFiles(prev => prev.map(f => ({ ...f, progress: 0, uploaded: false, error: undefined })))

      // Загружаем все файлы параллельно
      const uploadPromises = files.map(async (fileData, index) => {
        const uploadInfo = uploadUrls.find((u: any) => u.originalName === fileData.file.name)
        if (!uploadInfo?.uploadUrl) throw new Error(`No upload URL for ${fileData.file.name}`)
        
        const success = await uploadFileToMinio(fileData.file, uploadInfo.uploadUrl, index)
        if (success) {
          // Подтверждаем загрузку
          await fetch('/api/mix/confirm-upload', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
              taskId: newTaskId,
              fileName: fileData.file.name
            })
          })
        }
        return success
      })

      const results = await Promise.allSettled(uploadPromises)
      const failedUploads = results.filter(r => r.status === 'rejected')
      
      if (failedUploads.length > 0) {
        throw new Error(`Не удалось загрузить ${failedUploads.length} файлов`)
      }
      
      setStatus('Все файлы загружены, ожидание обработки...')
      setTaskStatus('processing')
      
      // Начинаем поллинг статуса
      pollInterval.current = setInterval(() => {
        pollTaskStatus(newTaskId)
      }, 2000)

    } catch (e: any) {
      setStatus('Ошибка')
      alert(e?.message || String(e))
      setTaskStatus('failed')
    }
  }

  const resetMixer = () => {
    if (pollInterval.current) {
      clearInterval(pollInterval.current)
      pollInterval.current = null
    }
    setFiles([])
    setTaskId(null)
    setTaskStatus('pending')
    setDownloadUrl(null)
    setStatus('Готово')
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  // Cleanup поллинга при размонтировании
  useEffect(() => {
    return () => {
      if (pollInterval.current) {
        clearInterval(pollInterval.current)
      }
    }
  }, [])

  const canModifySettings = taskStatus === 'pending' || taskStatus === 'failed'
  const isProcessing = taskStatus === 'uploading' || taskStatus === 'processing'

  return (
    <div className="min-h-screen p-6 bg-gradient-to-br from-blue-50 to-indigo-100">
      <div className="max-w-3xl mx-auto bg-white rounded-lg shadow p-6">
        <h1 className="text-2xl font-semibold mb-4">WBand Mixer (Async)</h1>

        <div className="mb-4">
          <label className="block text-sm mb-1">Input Files</label>
          <input 
            ref={fileInputRef} 
            type="file" 
            multiple 
            onChange={onAddFiles} 
            className="border rounded p-2 mb-3 w-full"
            disabled={isProcessing}
          />
          
          <div className="space-y-2 max-h-48 overflow-y-auto border rounded p-2">
            {files.map((fileData, i) => (
              <div key={i} className="flex items-center justify-between p-2 bg-gray-50 rounded">
                <div className="flex-1">
                  <div className="flex items-center justify-between mb-1">
                    <span className="text-sm truncate max-w-[70%]">{fileData.file.name}</span>
                    <button 
                      onClick={() => onRemove(i)} 
                      className="text-red-600 hover:text-red-800"
                      disabled={isProcessing}
                    >
                      ×
                    </button>
                  </div>
                  
                  {(taskStatus === 'uploading' || fileData.uploaded) && (
                    <div className="w-full bg-gray-200 rounded-full h-2">
                      <div 
                        className={`h-2 rounded-full ${fileData.uploaded ? 'bg-green-500' : 'bg-blue-500'}`}
                        style={{ width: `${fileData.progress}%` }}
                      />
                    </div>
                  )}
                  
                  {fileData.error && (
                    <div className="text-red-600 text-xs mt-1">{fileData.error}</div>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
          <div>
            <label className="block text-sm mb-1">Format</label>
            <select 
              value={format} 
              onChange={e => onFormatChange(e.target.value)} 
              className="border rounded p-2 w-full"
              disabled={!canModifySettings}
            >
              {formats.map(f => (
                <option key={f} value={f}>{f.toUpperCase()}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm mb-1">Output name</label>
            <input 
              value={outName} 
              onChange={e => setOutName(e.target.value)} 
              className="border rounded p-2 w-full"
              disabled={!canModifySettings}
            />
          </div>
        </div>

        <div className="flex items-center gap-4 mb-6">
          <label className="flex items-center gap-2">
            <input 
              type="checkbox" 
              checked={normalize} 
              onChange={e => setNormalize(e.target.checked)}
              disabled={!canModifySettings}
            />
            Normalize
          </label>
          <label className="flex items-center gap-2">
            <span>LUFS</span>
            <input 
              type="number" 
              step={0.5} 
              min={-40} 
              max={-5} 
              value={lufs} 
              onChange={e => setLufs(Number(e.target.value))} 
              className="border rounded p-1 w-24"
              disabled={!canModifySettings}
            />
          </label>
        </div>

        <div className="space-y-3">
          <button 
            disabled={files.length === 0 || isProcessing} 
            onClick={onMix} 
            className="w-full bg-indigo-600 text-white rounded py-3 disabled:opacity-50 hover:bg-indigo-700 disabled:hover:bg-indigo-600"
          >
            {taskStatus === 'uploading' ? 'Загрузка...' : 
             taskStatus === 'processing' ? 'Обработка...' : 'START MIX'}
          </button>

          {downloadUrl && (
            <a 
              href={downloadUrl} 
              download={outName}
              className="block w-full bg-green-600 text-white text-center rounded py-3 hover:bg-green-700"
            >
              Скачать результат
            </a>
          )}

          {(taskStatus === 'completed' || taskStatus === 'failed') && (
            <button 
              onClick={resetMixer}
              className="w-full bg-gray-500 text-white rounded py-2 hover:bg-gray-600"
            >
              Начать заново
            </button>
          )}
        </div>

        <div className="flex items-center justify-between text-sm text-gray-600 mt-3">
          <span>Статус: {status}</span>
          {taskId && <span>ID: {taskId.slice(0, 8)}...</span>}
        </div>
      </div>
    </div>
  )
}
