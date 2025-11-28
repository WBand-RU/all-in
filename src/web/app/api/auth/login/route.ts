import { NextRequest, NextResponse } from "next/server";
import { z } from "zod";
import dbConnect from "shared/lib/mongodb";
import { User } from "entities/user/model/user.model";
import { verifyPassword, signToken } from "shared/lib/auth";

const loginSchema = z.object({
    email: z.string().email(),
    password: z.string().min(1),
});

export async function POST(req: NextRequest) {
    try {
        await dbConnect();

        const body = await req.json();
        const { email, password } = loginSchema.parse(body);

        const user = await User.findOne({ email });
        if (!user) {
            return NextResponse.json(
                { error: "Неверный email или пароль" },
                { status: 401 }
            );
        }

        const isPasswordValid = await verifyPassword(password, user.password);
        if (!isPasswordValid) {
            return NextResponse.json(
                { error: "Неверный email или пароль" },
                { status: 401 }
            );
        }

        const token = signToken({
            userId: user._id,
            username: user.username,
            email: user.email,
        });

        const userResponse = {
            _id: user._id,
            username: user.username,
            email: user.email,
            firstName: user.firstName,
            lastName: user.lastName,
            avatar: user.avatar,
            createdAt: user.createdAt,
        };

        return NextResponse.json({
            user: userResponse,
            token,
        });
    } catch (error: any) {
        if (error.name === "ZodError") {
            return NextResponse.json(
                { error: "Неверные данные" },
                { status: 400 }
            );
        }

        return NextResponse.json(
            { error: "Внутренняя ошибка сервера" },
            { status: 500 }
        );
    }
}
