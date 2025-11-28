import { NextRequest, NextResponse } from 'next/server'
import { z } from 'zod'
import crypto from 'crypto'
import dbConnect from 'shared/lib/mongodb'
import { Band } from 'entities/band/model/band.model'
import { BandInvitation } from 'entities/band/model/invitation.model'
import { requireAuth } from 'shared/lib/middleware'

const inviteByEmailSchema = z.object({
  email: z.string().email('Неверный формат email'),
  role: z.string().min(1, 'Роль обязательна').default('viewer')
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
        { error: 'Недостаточно прав для просмотра приглашений' },
        { status: 403 }
      )
    }

    const invitations = await BandInvitation.find({ 
      bandId: params.id,
      status: 'pending'
    })
      .populate('invitedBy', 'username email')
      .sort({ createdAt: -1 })

    return NextResponse.json({ invitations })

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
    const { email, role } = inviteByEmailSchema.parse(body)

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
        { error: 'Недостаточно прав для приглашения участников' },
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

    const existingMember = band.members.find(m => 
      m.user.toString() === user.userId || 
      (m as any).email === email
    )
    if (existingMember) {
      return NextResponse.json(
        { error: 'Пользователь уже является участником группы' },
        { status: 409 }
      )
    }

    const existingInvitation = await BandInvitation.findOne({
      bandId: params.id,
      email,
      status: 'pending'
    })
    if (existingInvitation) {
      return NextResponse.json(
        { error: 'Приглашение уже отправлено' },
        { status: 409 }
      )
    }

    const token = crypto.randomBytes(32).toString('hex')
    
    const invitation = new BandInvitation({
      bandId: params.id,
      invitedBy: user.userId,
      email,
      role,
      token
    })

    await invitation.save()
    await invitation.populate('invitedBy', 'username email')
    await invitation.populate('bandId', 'name')

    // TODO: Отправить email с приглашением
    // await sendInvitationEmail(invitation)

    return NextResponse.json({ invitation }, { status: 201 })

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
