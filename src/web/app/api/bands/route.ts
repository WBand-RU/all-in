import { NextRequest, NextResponse } from 'next/server'
import { z } from 'zod'
import dbConnect from 'shared/lib/mongodb'
import { Band } from 'entities/band/model/band.model'
import { requireAuth } from 'shared/lib/middleware'

const createBandSchema = z.object({
  name: z.string().min(1).max(100),
  description: z.string().max(500).optional()
})

export async function GET(req: NextRequest) {
  try {
    const user = requireAuth(req)
    if (user instanceof NextResponse) return user

    await dbConnect()

    const bands = await Band.find({
      $or: [
        { owner: user.userId },
        { 'members.user': user.userId, 'members.status': 'active' }
      ]
    }).populate('owner', 'username email firstName lastName')
      .populate('members.user', 'username email firstName lastName')

    return NextResponse.json({ bands })

  } catch (error) {
    return NextResponse.json(
      { error: 'Внутренняя ошибка сервера' },
      { status: 500 }
    )
  }
}

export async function POST(req: NextRequest) {
  try {
    const user = requireAuth(req)
    if (user instanceof NextResponse) return user

    await dbConnect()

    const body = await req.json()
    const { name, description } = createBandSchema.parse(body)

    const band = new Band({
      name,
      description,
      owner: user.userId,
      members: [{
        user: user.userId,
        role: 'owner',
        status: 'active'
      }]
    })

    await band.save()
    await band.populate('owner', 'username email firstName lastName')
    await band.populate('members.user', 'username email firstName lastName')

    return NextResponse.json({ band }, { status: 201 })

  } catch (error: any) {
    if (error.name === 'ZodError') {
      return NextResponse.json(
        { error: 'Неверные данные', details: error.errors },
        { status: 400 }
      )
    }

    return NextResponse.json(
      { error: 'Внутренняя ошибка сервера' },
      { status: 500 }
    )
  }
}
