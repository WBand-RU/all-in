import { Link } from "react-router";
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from "@/shared/ui/card";

import {
    Music,
    ArrowRight,
    Calendar,
    ListMusic,
    MessageSquare,
    Mic2,
} from "lucide-react";

const stats = [
    {
        title: "100+",
        description: "групп используют приложение",
    },
    {
        title: "10K+",
        description: "песен в базе данных",
    },
    {
        title: "24/7",
        description: "доступ к вашему репертуару",
    },
];

const features = [
    {
        icon: ListMusic,
        title: "База песен и плейбеков",
        description:
            "Храните весь репертуар в одном месте. Добавляйте треки, тексты песен, аккорды, плейбеки и нотные материалы.",
    },
    {
        icon: Calendar,
        title: "Планирование мероприятий",
        description:
            "Организуйте выступления, репетиции и концерты. Управляйте расписанием группы и отслеживайте важные даты.",
    },
    {
        icon: MessageSquare,
        title: "Общение и координация",
        description:
            "Обсуждайте сет-листы, договаривайтесь о деталях выступлений и делитесь идеями прямо в приложении.",
    },
    {
        icon: Mic2,
        title: "Управление составом",
        description:
            "Добавляйте участников группы, распределяйте роли, отслеживайте доступность музыкантов для мероприятий.",
    },
];

const steps = [
    {
        title: "Создайте группу",
        description:
            "Зарегистрируйтесь, создайте свою группу и пригласите участников для совместной работы.",
    },
    {
        title: "Наполните базу",
        description:
            "Добавьте песни, плейбеки, тексты и ноты. Организуйте репертуар по альбомам, жанрам или сет-листам.",
    },
    {
        title: "Планируйте выступления",
        description:
            "Создавайте мероприятия, формируйте программу выступления, координируйте подготовку и репетиции.",
    },
];

export function HomePage() {
    return (
        <main className="flex flex-col gap-20 bg-gradient-to-b from-slate-50 via-white to-white">
            <section className="relative overflow-hidden">
                <div className="mx-auto max-w-6xl px-6 pb-24 pt-20">
                    <div className="relative rounded-3xl bg-gradient-to-br from-blue-600 via-indigo-600 to-purple-600 px-10 py-16 text-white shadow-xl">
                        <div className="flex max-w-3xl flex-col gap-8">
                            <div className="inline-flex items-center gap-2 rounded-full bg-white/10 px-4 py-1 text-sm font-medium backdrop-blur">
                                <Music className="h-4 w-4" />
                                Цифровой помощник музыкантов
                            </div>
                            <div className="space-y-4">
                                <h1 className="text-4xl font-semibold leading-tight sm:text-5xl md:text-6xl">
                                    WBand: всё для подготовки и выступлений
                                    вашей группы
                                </h1>
                                <p className="text-lg text-white/85">
                                    Управляйте репертуаром, планируйте
                                    мероприятия, храните плейбеки и тексты
                                    песен, координируйте работу группы в одном
                                    приложении.
                                </p>
                            </div>
                            <div className="flex flex-wrap items-center gap-4">
                                <Link
                                    to="/app"
                                    className="inline-flex items-center gap-2 rounded-lg bg-gray-800 px-5 py-3 text-sm font-semibold text-white transition-transform hover:-translate-y-0.5 hover:bg-gray-600"
                                >
                                    Начать работу
                                </Link>

                                <Link
                                    to="/about"
                                    className="inline-flex items-center gap-2 rounded-lg bg-white px-5 py-3 text-sm font-semibold text-blue-600 transition-transform hover:-translate-y-0.5 hover:bg-blue-50"
                                >
                                    Подробнее о возможностях
                                    <ArrowRight className="h-4 w-4" />
                                </Link>
                            </div>
                        </div>
                    </div>
                </div>
            </section>

            <section className="mx-auto w-full max-w-6xl px-6">
                <div className="grid gap-6 md:grid-cols-3">
                    {stats.map((stat) => (
                        <Card
                            key={stat.title}
                            className="border-none bg-white shadow-md"
                        >
                            <CardHeader>
                                <CardTitle className="text-4xl font-semibold text-blue-600">
                                    {stat.title}
                                </CardTitle>
                                <CardDescription className="text-base text-slate-600">
                                    {stat.description}
                                </CardDescription>
                            </CardHeader>
                        </Card>
                    ))}
                </div>
            </section>

            <section className="mx-auto max-w-6xl px-6">
                <div className="flex flex-col gap-12">
                    <div className="space-y-3 text-center">
                        <h2 className="text-4xl font-semibold">
                            Возможности приложения
                        </h2>
                        <p className="text-base text-slate-600">
                            Комплексное решение для организации работы
                            музыкальной группы и подготовки к выступлениям
                        </p>
                    </div>
                    <div className="grid gap-6 md:grid-cols-2">
                        {features.map((feature) => (
                            <Card
                                key={feature.title}
                                className="h-full w-full border-none bg-white shadow-md"
                            >
                                <CardHeader className="flex flex-row items-start gap-4">
                                    <feature.icon className="h-10 w-10 text-blue-600" />
                                    <div className="space-y-2">
                                        <CardTitle className="text-xl font-semibold">
                                            {feature.title}
                                        </CardTitle>
                                        <CardDescription className="text-sm text-slate-600">
                                            {feature.description}
                                        </CardDescription>
                                    </div>
                                </CardHeader>
                            </Card>
                        ))}
                    </div>
                </div>
            </section>

            <section className="mx-auto w-full max-w-6xl px-6">
                <div className="grid gap-6 md:grid-cols-3">
                    {steps.map((step, index) => (
                        <Card
                            key={step.title}
                            className="relative h-full w-full border-none bg-gradient-to-br from-slate-100 to-white shadow-inner"
                        >
                            <CardHeader className="space-y-4">
                                <div className="inline-flex h-12 w-12 items-center justify-center rounded-full bg-blue-600 text-lg font-semibold text-white">
                                    {index + 1}
                                </div>
                                <CardTitle className="text-xl font-semibold">
                                    {step.title}
                                </CardTitle>
                                <CardDescription className="text-sm text-slate-600">
                                    {step.description}
                                </CardDescription>
                            </CardHeader>
                        </Card>
                    ))}
                </div>
            </section>

            <section className="mx-auto max-w-6xl px-6 pb-24">
                <Card className="border-none bg-gradient-to-br from-purple-600 to-blue-600 text-white shadow-xl">
                    <CardContent className="flex flex-col gap-6 p-10 md:flex-row md:items-center md:justify-between">
                        <div className="space-y-3">
                            <h3 className="text-3xl font-semibold">
                                Готовы вывести вашу группу на новый уровень?
                            </h3>
                            <p className="text-white/80">
                                Присоединяйтесь к WBand и управляйте всеми
                                аспектами работы группы в одном месте.
                            </p>
                        </div>
                        <Link
                            to="/bands"
                            className="inline-flex items-center gap-2 rounded-lg bg-white px-6 py-3 text-sm font-semibold text-blue-600 transition-transform hover:-translate-y-0.5 hover:bg-blue-50"
                        >
                            Создать группу
                            <ArrowRight className="h-4 w-4" />
                        </Link>
                    </CardContent>
                </Card>
            </section>
        </main>
    );
}
