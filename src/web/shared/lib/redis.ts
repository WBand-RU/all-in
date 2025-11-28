import { createClient } from 'redis'

const client = createClient({
  url: process.env.REDIS_URL || 'redis://localhost:6379'
})

client.on('error', (err) => console.log('Redis Client Error', err))

let isConnected = false

export const getRedisClient = async () => {
  if (!isConnected) {
    await client.connect()
    isConnected = true
  }
  return client
}

export const addToQueue = async (queueName: string, data: any) => {
  const redis = await getRedisClient()
  await redis.rPush(queueName, JSON.stringify(data))
}
