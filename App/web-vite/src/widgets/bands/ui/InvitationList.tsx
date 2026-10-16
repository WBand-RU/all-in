import { ClockIcon, CheckIcon, XIcon, MailIcon } from "lucide-react";
import { Badge } from "@/shared/ui/badge";
import { Card, CardContent } from "@/shared/ui/card";
import { useGetBandInvitationList } from "@/lib/generated-api/band-api/invitations";
import { ApiCodes } from "@/lib/generated-api/band-api/models";
import { Skeleton } from "@/shared/ui/skeleton";
import { useTranslation } from "react-i18next";

interface InvitationCardProps {
    invitation: any; // TODO: Use proper API type once generated
}

export function InvitationCard({ invitation }: InvitationCardProps) {
    const { t } = useTranslation();
    const getStatusIcon = (status: string) => {
        switch (status) {
            case "Pending":
                return <ClockIcon className="h-4 w-4" />;
            case "Accepted":
                return <CheckIcon className="h-4 w-4" />;
            case "Declined":
                return <XIcon className="h-4 w-4" />;
            default:
                return <MailIcon className="h-4 w-4" />;
        }
    };

    return (
        <Card>
            <CardContent>
                <div className="flex items-start justify-between">
                    <div className="space-y-2">
                        <div className="flex items-center gap-2">
                            <MailIcon className="h-4 w-4 text-muted-foreground" />
                            <span className="font-medium">
                                {invitation.inviteeEmail}
                            </span>
                            <Badge variant="outline" className="text-xs">
                                {t(`band.roles.${invitation.role}`)}
                            </Badge>
                        </div>
                        <div className="flex items-center gap-2 text-sm text-muted-foreground">
                            <span>{t("invitations.invitedBy", { email: invitation.inviterId })}</span>
                        </div>
                    </div>
                    <div className="flex items-center gap-2">
                        <Badge
                            variant={
                                invitation.status === "Pending"
                                    ? "secondary"
                                    : "outline"
                            }
                            className="flex items-center gap-1"
                        >
                            {getStatusIcon(invitation.status)}
                            {t(`invitations.${invitation.status.toLowerCase()}`)}
                        </Badge>
                    </div>
                </div>
            </CardContent>
        </Card>
    );
}

// Real data component using API
export function InvitationList({ bandId }: { bandId: string }) {
    const { t } = useTranslation();
    const { data, isLoading, error } = useGetBandInvitationList(bandId);

    if (isLoading) {
        return (
            <div className="space-y-3">
                {Array.from({ length: 3 }).map((_, i) => (
                    <Card key={i}>
                        <CardContent className="pt-6">
                            <div className="flex items-start justify-between">
                                <div className="space-y-2">
                                    <Skeleton className="h-4 w-32" />
                                    <Skeleton className="h-3 w-24" />
                                </div>
                                <div className="flex items-center gap-2">
                                    <Skeleton className="h-6 w-16" />
                                </div>
                            </div>
                        </CardContent>
                    </Card>
                ))}
            </div>
        );
    }

    if (error || !data || data.code !== ApiCodes.Success) {
        return (
            <Card>
                <CardContent className="pt-6">
                    <p className="text-muted-foreground text-center">
                        {t("invitations.loadFailed")}
                    </p>
                </CardContent>
            </Card>
        );
    }

    const invitations = data?.value || [];

    const pendingInvitations = invitations.filter(
        (inv: any) => inv.status === "Pending",
    );
    const otherInvitations = invitations.filter(
        (inv: any) => inv.status !== "Pending",
    );

    return (
        <div className="space-y-4">
            {pendingInvitations.length > 0 && (
                <div className="space-y-3">
                    <h4 className="text-sm font-medium text-muted-foreground uppercase tracking-wide">
                        {t("invitations.pendingTitle")}
                    </h4>
                    {pendingInvitations.map((invitation: any) => (
                        <InvitationCard
                            key={invitation.id}
                            invitation={invitation}
                        />
                    ))}
                </div>
            )}

            {otherInvitations.length > 0 && (
                <div className="space-y-3">
                    <h4 className="text-sm font-medium text-muted-foreground uppercase tracking-wide">
                        {t("invitations.recentTitle")}
                    </h4>
                    {otherInvitations.map((invitation: any) => (
                        <InvitationCard
                            key={invitation.id}
                            invitation={invitation}
                        />
                    ))}
                </div>
            )}

            {invitations.length === 0 && (
                <Card>
                    <CardContent className="pt-6">
                        <p className="text-muted-foreground text-center">
                            {t("invitations.emptyBand")}
                        </p>
                    </CardContent>
                </Card>
            )}
        </div>
    );
}
