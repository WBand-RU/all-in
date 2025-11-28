import { NextRequest, NextResponse } from 'next/server'
import { z } from 'zod'
import dbConnect from 'shared/lib/mongodb'
import { Band } from 'entities/band/model/band.model'
import { BandInvitation, InviteToken } from 'entities/band/model/invitation.model'
import { requireAuth } from 'shared/lib/middleware'

const respondToInvitationSchema = z.object({
  action: z.enum(['accept', 'decline'])
})

export async function GET(req: NextRequest, { params }: { params: { token: string } }) {
  try {
    await dbConnect()

    const invitation = await BandInvitation.findOne({ 
      token: params.token,
      status: 'pending',
      expiresAt: { $gt: new Date() }
    })
      .populate('bandId', 'name description')
      .populate('invitedBy', 'username')

    if (!invitation) {
      return NextResponse.json(
        { error: 'Приглашение не найдено или истекло' },
        { status: 404 }
      )
    }

    return NextResponse.json({ invitation })

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

    const body = await req.json()
    const { action } = respondToInvitationSchema.parse(body)

    const invitation = await BandInvitation.findOne({ 
      token: params.token,
      status: 'pending',
      expiresAt: { $gt: new Date() }
    })

    if (!invitation) {
      return NextResponse.json(
        { error: 'Приглашение не найдено или истекло' },
        { status: 404 }
      )
    }

    if (action === 'accept') {
      const band = await Band.findById(invitation.bandId)
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
        role: invitation.role,
        status: 'active'
      })

      await band.save()
      invitation.status = 'accepted'
    } else {
      invitation.status = 'declined'
    }

    await invitation.save()

    return NextResponse.json({ 
      success: true, 
      status: invitation.status 
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
