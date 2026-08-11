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
import { useTranslation } from "react-i18next";

const stats = [
    {
        title: "100+",
        description: "home.stats.0",
    },
    {
        title: "10K+",
        description: "home.stats.1",
    },
    {
        title: "24/7",
        description: "home.stats.2",
    },
];

const features = [
    {
        icon: ListMusic,
        title: "home.features.0.title",
        description: "home.features.0.text",
    },
    {
        icon: Calendar,
        title: "home.features.1.title",
        description: "home.features.1.text",
    },
    {
        icon: MessageSquare,
        title: "home.features.2.title",
        description: "home.features.2.text",
    },
    {
        icon: Mic2,
        title: "home.features.3.title",
        description: "home.features.3.text",
    },
];

const steps = [
    {
        title: "home.steps.0.title",
        description: "home.steps.0.text",
    },
    {
        title: "home.steps.1.title",
        description: "home.steps.1.text",
    },
    {
        title: "home.steps.2.title",
        description: "home.steps.2.text",
    },
];

export function HomePage() {
    const { t } = useTranslation();
    return (
        <main className="flex flex-col gap-20 bg-gradient-to-b from-slate-50 via-white to-white">
            <section className="relative overflow-hidden">
                <div className="mx-auto max-w-6xl px-6 pb-24 pt-20">
                    <div className="relative rounded-3xl bg-gradient-to-br from-blue-600 via-indigo-600 to-purple-600 px-10 py-16 text-white shadow-xl">
                        <div className="flex max-w-3xl flex-col gap-8">
                            <div className="inline-flex items-center gap-2 rounded-full bg-white/10 px-4 py-1 text-sm font-medium backdrop-blur">
                                <Music className="h-4 w-4" />
                                {t("home.badge")}
                            </div>
                            <div className="space-y-4">
                                <h1 className="text-4xl font-semibold leading-tight sm:text-5xl md:text-6xl">
                                    {t("home.title")}
                                </h1>
                                <p className="text-lg text-white/85">
                                    {t("home.subtitle")}
                                </p>
                            </div>
                            <div className="flex flex-wrap items-center gap-4">
                                <Link
                                    to="/app"
                                    className="inline-flex items-center gap-2 rounded-lg bg-gray-800 px-5 py-3 text-sm font-semibold text-white transition-transform hover:-translate-y-0.5 hover:bg-gray-600"
                                >
                                    {t("home.start")}
                                </Link>

                                <Link
                                    to="/about"
                                    className="inline-flex items-center gap-2 rounded-lg bg-white px-5 py-3 text-sm font-semibold text-blue-600 transition-transform hover:-translate-y-0.5 hover:bg-blue-50"
                                >
                                    {t("home.learn")}
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
                                    {t(stat.description)}
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
                            {t("home.featuresTitle")}
                        </h2>
                        <p className="text-base text-slate-600">
                            {t("home.featuresSubtitle")}
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
                                            {t(feature.title)}
                                        </CardTitle>
                                        <CardDescription className="text-sm text-slate-600">
                                            {t(feature.description)}
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
                                    {t(step.title)}
                                </CardTitle>
                                <CardDescription className="text-sm text-slate-600">
                                    {t(step.description)}
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
                                {t("home.ctaTitle")}
                            </h3>
                            <p className="text-white/80">
                                {t("home.ctaText")}
                            </p>
                        </div>
                        <Link
                            to="/bands"
                            className="inline-flex items-center gap-2 rounded-lg bg-white px-6 py-3 text-sm font-semibold text-blue-600 transition-transform hover:-translate-y-0.5 hover:bg-blue-50"
                        >
                            {t("home.createBand")}
                            <ArrowRight className="h-4 w-4" />
                        </Link>
                    </CardContent>
                </Card>
            </section>
        </main>
    );
}
