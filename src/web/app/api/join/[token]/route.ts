import { NextRequest, NextResponse } from 'next/server'
import dbConnect from 'shared/lib/mongodb'
import { Band } from 'entities/band/model/band.model'
import { InviteToken } from 'entities/band/model/invitation.model'
import { requireAuth } from 'shared/lib/middleware'

export async function GET(req: NextRequest, { params }: { params: { token: string } }) {
  try {
    await dbConnect()

    const inviteToken = await InviteToken.findOne({ 
      token: params.token,
      isActive: true,
      expiresAt: { $gt: new Date() },
      $expr: { $lt: ['$currentUses', '$maxUses'] }
    })
      .populate('bandId', 'name description')
      .populate('createdBy', 'username')

    if (!inviteToken) {
      return NextResponse.json(
        { error: 'Ссылка недействительна или истекла' },
        { status: 404 }
      )
    }

    return NextResponse.json({ inviteToken })

  } catch (error) {
    return NextResponse.json(
      { error: 'Внутренняя ошибка сервера' },
      { status: 500 }
    )
  }
}

export async function POST(req: NextRequest, { params }: { params: { token: string } }) {
  try {
    const user = requireAuth(req)
    if (user instanceof NextResponse) return user

    await dbConnect()

    const inviteToken = await InviteToken.findOne({ 
      token: params.token,
      isActive: true,
      expiresAt: { $gt: new Date() },
      $expr: { $lt: ['$currentUses', '$maxUses'] }
    })

    if (!inviteToken) {
      return NextResponse.json(
        { error: 'Ссылка недействительна или истекла' },
        { status: 404 }
      )
    }

    const band = await Band.findById(inviteToken.bandId)
    if (!band) {
      return NextResponse.json(
        { error: 'Группа не найдена' },
        { status: 404 }
      )
    }

    const existingMember = band.members.find(m => m.user.toString() === user.userId)
    if (existingMember) {
      return NextResponse.json(
        { error: 'Вы уже являетесь участником группы' },
        { status: 409 }
      )
    }

    band.members.push({
      user: user.userId as any,
      role: inviteToken.role,
      status: 'active'
    })

    inviteToken.currentUses += 1

    await Promise.all([
      band.save(),
      inviteToken.save()
    ])

    await band.populate('owner', 'username email')
    await band.populate('members.user', 'username email')

    return NextResponse.json({ 
      success: true,
      band,
      message: `Вы успешно присоединились к группе "${band.name}"` 
    })

  } catch (error) {
    return NextResponse.json(
      { error: 'Внутренняя ошибка сервера' },
      { status: 500 }
    )
  }
}
