import { NextRequest, NextResponse } from 'next/server'
import { z } from 'zod'
import crypto from 'crypto'
import dbConnect from 'shared/lib/mongodb'
import { Band } from 'entities/band/model/band.model'
import { InviteToken } from 'entities/band/model/invitation.model'
import { requireAuth } from 'shared/lib/middleware'

const createTokenSchema = z.object({
  role: z.string().min(1, 'Роль обязательна').default('viewer'),
  maxUses: z.number().min(1).max(100).default(1),
  expiresInDays: z.number().min(1).max(30).default(7)
})

export async function GET(req: NextRequest, { params }: { params: { id: string } }) {
  try {
    const user = requireAuth(req)
    if (user instanceof NextResponse) return user

    await dbConnect()

    const band = await Band.findById(params.id)
    if (!band) {
      return NextResponse.json(
        { error: 'Группа не найдена' },
        { status: 404 }
      )
    }

    const member = band.members.find(m => m.user.toString() === user.userId)
    if (!member) {
      return NextResponse.json(
        { error: 'Нет доступа к группе' },
        { status: 403 }
      )
    }

    const memberRole = band.roles.find(r => r.name === member.role)
    if (!memberRole?.permissions.includes('manage_members')) {
      return NextResponse.json(
        { error: 'Недостаточно прав для просмотра токенов' },
        { status: 403 }
      )
    }

    const tokens = await InviteToken.find({ 
      bandId: params.id,
      isActive: true,
      expiresAt: { $gt: new Date() }
    })
      .populate('createdBy', 'username email')
      .sort({ createdAt: -1 })

    return NextResponse.json({ tokens })

  } catch (error) {
    return NextResponse.json(
      { error: 'Внутренняя ошибка сервера' },
      { status: 500 }
    )
  }
}

export async function POST(req: NextRequest, { params }: { params: { id: string } }) {
  try {
    const user = requireAuth(req)
    if (user instanceof NextResponse) return user

    await dbConnect()

    const body = await req.json()
    const { role, maxUses, expiresInDays } = createTokenSchema.parse(body)

    const band = await Band.findById(params.id)
    if (!band) {
      return NextResponse.json(
        { error: 'Группа не найдена' },
        { status: 404 }
      )
    }

    const member = band.members.find(m => m.user.toString() === user.userId)
    if (!member) {
      return NextResponse.json(
        { error: 'Нет доступа к группе' },
        { status: 403 }
      )
    }

    const memberRole = band.roles.find(r => r.name === member.role)
    if (!memberRole?.permissions.includes('manage_members')) {
      return NextResponse.json(
        { error: 'Недостаточно прав для создания токенов' },
        { status: 403 }
      )
    }

    const targetRole = band.roles.find(r => r.name === role)
    if (!targetRole) {
      return NextResponse.json(
        { error: 'Указанная роль не существует в группе' },
        { status: 400 }
      )
    }

    const token = crypto.randomBytes(32).toString('hex')
    const expiresAt = new Date(Date.now() + expiresInDays * 24 * 60 * 60 * 1000)
    
    const inviteToken = new InviteToken({
      bandId: params.id,
      createdBy: user.userId,
      token,
      role,
      maxUses,
      expiresAt
    })

    await inviteToken.save()
    await inviteToken.populate('createdBy', 'username email')
    await inviteToken.populate('bandId', 'name')

    return NextResponse.json({ inviteToken }, { status: 201 })

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

export async function DELETE(req: NextRequest, { params }: { params: { id: string } }) {
  try {
    const user = requireAuth(req)
    if (user instanceof NextResponse) return user

    const url = new URL(req.url)
    const tokenId = url.searchParams.get('tokenId')

    if (!tokenId) {
      return NextResponse.json(
        { error: 'ID токена обязателен' },
        { status: 400 }
      )
    }

    await dbConnect()

    const band = await Band.findById(params.id)
    if (!band) {
      return NextResponse.json(
        { error: 'Группа не найдена' },
        { status: 404 }
      )
    }

    const member = band.members.find(m => m.user.toString() === user.userId)
    if (!member) {
      return NextResponse.json(
        { error: 'Нет доступа к группе' },
        { status: 403 }
      )
    }

    const memberRole = band.roles.find(r => r.name === member.role)
    if (!memberRole?.permissions.includes('manage_members')) {
      return NextResponse.json(
        { error: 'Недостаточно прав для удаления токенов' },
        { status: 403 }
      )
    }

    const result = await InviteToken.findByIdAndUpdate(
      tokenId,
      { isActive: false },
      { new: true }
    )

    if (!result) {
      return NextResponse.json(
        { error: 'Токен не найден' },
        { status: 404 }
      )
    }

    return NextResponse.json({ success: true })

  } catch (error) {
    return NextResponse.json(
      { error: 'Внутренняя ошибка сервера' },
      { status: 500 }
    )
  }
}
