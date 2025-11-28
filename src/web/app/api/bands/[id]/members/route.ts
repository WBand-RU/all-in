import { NextRequest, NextResponse } from 'next/server'
import { z } from 'zod'
import dbConnect from 'shared/lib/mongodb'
import { Band } from 'entities/band/model/band.model'
import { User } from 'entities/user/model/user.model'
import { requireAuth } from 'shared/lib/middleware'

const inviteMemberSchema = z.object({
  email: z.string().email(),
  role: z.string().default('viewer')
})

export async function POST(req: NextRequest, { params }: { params: { id: string } }) {
  try {
    const user = requireAuth(req)
    if (user instanceof NextResponse) return user

    await dbConnect()

    const body = await req.json()
    const { email, role } = inviteMemberSchema.parse(body)

    const band = await Band.findById(params.id)
    if (!band) {
      return NextResponse.json(
        { error: 'Группа не найдена' },
        { status: 404 }
      )
    }

    const userMember = band.members.find(m => m.user.toString() === user.userId)
    if (!userMember || !band.roles.find(r => r.name === userMember.role)?.permissions.includes('manage_members')) {
      return NextResponse.json(
        { error: 'Недостаточно прав' },
        { status: 403 }
      )
    }

    const invitedUser = await User.findOne({ email })
    if (!invitedUser) {
      return NextResponse.json(
        { error: 'Пользователь с таким email не найден' },
        { status: 404 }
      )
    }

    const existingMember = band.members.find(m => m.user.toString() === invitedUser._id.toString())
    if (existingMember) {
      return NextResponse.json(
        { error: 'Пользователь уже является участником группы' },
        { status: 400 }
      )
    }

    band.members.push({
      user: invitedUser._id,
      role,
      status: 'invited',
      joinedAt: new Date()
    })

    await band.save()
    await band.populate('members.user', 'username email firstName lastName')

    return NextResponse.json({
      message: 'Приглашение отправлено',
      member: band.members[band.members.length - 1]
    })

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
