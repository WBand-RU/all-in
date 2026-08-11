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
                    <h2 className="text-lg font-semibold mb-4">
                        {t("dashboard.invitations")}
                    </h2>
                    <MyInvitations />
                </div>

                {/* Future dashboard widgets can go here */}
                <div className="space-y-4">
                    <div className="p-4 border rounded-lg">
                        <h3 className="font-medium">{t("dashboard.quickActions")}</h3>
                        <p className="text-sm text-muted-foreground mt-1">
                            {t("dashboard.quickActionsHint")}
                        </p>
                    </div>
                </div>
            </div>
        </div>
    );
}
