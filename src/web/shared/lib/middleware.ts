import { NextRequest, NextResponse } from 'next/server'
import { verifyToken } from './auth'

export const authMiddleware = (req: NextRequest) => {
  const token = req.headers.get('authorization')?.replace('Bearer ', '')
  
  if (!token) {
    return null
  }

  try {
    const decoded = verifyToken(token)
    return decoded
  } catch (error) {
    return null
  }
}

export const requireAuth = (req: NextRequest) => {
  const user = authMiddleware(req)
  
  if (!user) {
    return NextResponse.json(
      { error: 'Требуется авторизация' },
      { status: 401 }
    )
  }
  
  return user
}
