import { MyInvitations } from "@/widgets/bands/ui/MyInvitations";
import { useTranslation } from "react-i18next";

export function MyInvitationsPage() {
    const { t } = useTranslation();
    return (
        <div className="space-y-6">
            <div>
                <h1 className="text-2xl font-bold">{t("invitations.title")}</h1>
                <p className="text-muted-foreground">
                    {t("invitations.subtitle")}
                </p>
            </div>

            <MyInvitations />
        </div>
    );
}
