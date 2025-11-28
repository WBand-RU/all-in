import { NextRequest, NextResponse } from 'next/server'
import { z } from 'zod'
import dbConnect from 'shared/lib/mongodb'
import { User } from 'entities/user/model/user.model'
import { hashPassword, signToken } from 'shared/lib/auth'

const registerSchema = z.object({
  username: z.string().min(3).max(20),
  email: z.string().email(),
  password: z.string().min(6),
  firstName: z.string().optional(),
  lastName: z.string().optional()
})

export async function POST(req: NextRequest) {
  try {
    await dbConnect()

    const body = await req.json()
    const validatedData = registerSchema.parse(body)

    const existingUser = await User.findOne({
      $or: [
        { email: validatedData.email },
        { username: validatedData.username }
      ]
    })

    if (existingUser) {
      return NextResponse.json(
        { error: 'Пользователь уже существует' },
        { status: 400 }
      )
    }

    const hashedPassword = await hashPassword(validatedData.password)

    const user = new User({
      username: validatedData.username,
      email: validatedData.email,
      password: hashedPassword,
      firstName: validatedData.firstName,
      lastName: validatedData.lastName
    })

    await user.save()

    const token = signToken({
      userId: user._id,
      username: user.username,
      email: user.email
    })

    const userResponse = {
      _id: user._id,
      username: user.username,
      email: user.email,
      firstName: user.firstName,
      lastName: user.lastName,
      avatar: user.avatar,
      createdAt: user.createdAt
    }

    return NextResponse.json({
      user: userResponse,
      token
    }, { status: 201 })

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
