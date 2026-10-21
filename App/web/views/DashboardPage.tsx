"use client";

import { MyInvitations } from "@/widgets/bands/ui/MyInvitations";
import { useTranslation } from "react-i18next";

export function DashboardPage() {
    const { t } = useTranslation();
    return (
        <div className="flex flex-col gap-6">
            <div>
                <h1 className="text-2xl font-bold">{t("dashboard.title")}</h1>
                <p className="text-muted-foreground">
                    {t("dashboard.welcome")}
                </p>
            </div>

            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                <div className="md:col-span-2 lg:col-span-2">
                    <h2 className="mb-4 text-lg font-semibold">
                        {t("dashboard.invitations")}
                    </h2>
                    <MyInvitations />
                </div>

                {/* Future dashboard widgets can go here */}
                <div className="space-y-4">
                    <div className="rounded-lg border p-4">
                        <h3 className="font-medium">
                            {t("dashboard.quickActions")}
                        </h3>
                        <p className="mt-1 text-sm text-muted-foreground">
                            {t("dashboard.quickActionsHint")}
                        </p>
                    </div>
                </div>
            </div>
        </div>
    );
}
