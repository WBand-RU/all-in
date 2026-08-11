import { useParams } from "react-router";
import { useGetBand } from "@/lib/generated-api/band-api/bands";
import { Card, CardContent, CardHeader, CardTitle } from "@/shared/ui/card";
import { Badge } from "@/shared/ui/badge";
import { Button } from "@/shared/ui/button";
import {
    ArrowLeftIcon,
    CalendarIcon,
    UserIcon,
    UsersIcon,
    MailIcon,
} from "lucide-react";
import { useNavigator } from "@/services/navigator";
import { ApiCodes } from "@/lib/generated-api/band-api/models";
import { Skeleton } from "@/shared/ui/skeleton";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/shared/ui/tabs";
import { InviteMember } from "../widgets/bands/ui/InviteMember";
import { MemberList } from "../widgets/bands/ui/MemberList";
import { InvitationList } from "../widgets/bands/ui/InvitationList";
import { useBandAccess } from "@/hooks/use-band-access";
import { useTranslation } from "react-i18next";

export function BandViewPage() {
    const { id } = useParams<{ id: string }>();
    const { data, isLoading } = useGetBand(id!);
    const { go } = useNavigator();
    const { access } = useBandAccess();
    const { t, i18n } = useTranslation();

    if (isLoading) {
        return (
            <div className="space-y-6">
                <div className="flex items-center gap-4">
                    <Skeleton className="h-10 w-10" />
                    <Skeleton className="h-8 w-48" />
                </div>
                <Card>
                    <CardHeader>
                        <Skeleton className="h-6 w-32" />
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <Skeleton className="h-4 w-full" />
                        <Skeleton className="h-4 w-3/4" />
                        <Skeleton className="h-4 w-1/2" />
                    </CardContent>
                </Card>
            </div>
        );
    }

    if (!data || data.code !== ApiCodes.Success) {
        return (
            <div className="space-y-6">
                <div className="flex items-center gap-4">
                    <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => go("..")}
                    >
                        <ArrowLeftIcon className="h-4 w-4" />
                    </Button>
                    <h1 className="text-2xl font-bold">{t("band.notFound")}</h1>
                </div>
                <Card>
                    <CardContent className="pt-6">
                        <p className="text-muted-foreground">
                            {t("band.notFoundHint")}
                        </p>
                    </CardContent>
                </Card>
            </div>
        );
    }

    const band = data.value!;
    const isOwner = access.some(item => item.bandId === band.id && item.role === "Owner");

    return (
        <div className="space-y-6">
            <div className="flex items-center gap-4">
                <Button variant="ghost" size="icon" onClick={() => go("..")}>
                    <ArrowLeftIcon className="h-4 w-4" />
                </Button>
                <h1 className="text-2xl font-bold">{band.name}</h1>
                <Badge variant="outline">{t("common.band")}</Badge>
            </div>

            <div className="grid gap-6 md:grid-cols-2">
                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <UserIcon className="h-5 w-5" />
                            {t("band.information")}
                        </CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <div className="flex justify-between">
                            <span className="text-sm font-medium text-muted-foreground">
                                {t("common.name")}
                            </span>
                            <span>{band.name}</span>
                        </div>
                        <div className="flex justify-between">
                            <span className="text-sm font-medium text-muted-foreground">
                                ID
                            </span>
                            <span className="font-mono text-sm">{band.id}</span>
                        </div>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <CalendarIcon className="h-5 w-5" />
                            {t("band.creationDetails")}
                        </CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <div className="flex justify-between">
                            <span className="text-sm font-medium text-muted-foreground">
                                {t("band.createdAt")}
                            </span>
                            <span>
                                {new Date(band.createdAt).toLocaleDateString(i18n.resolvedLanguage)}
                            </span>
                        </div>
                        <div className="flex justify-between">
                            <span className="text-sm font-medium text-muted-foreground">
                                {t("band.createdBy")}
                            </span>
                            <span className="font-mono text-sm">
                                {band.createdBy}
                            </span>
                        </div>
                    </CardContent>
                </Card>
            </div>

            {/* Member Management */}
            <Tabs defaultValue="members" className="w-full">
                <TabsList className="grid w-full grid-cols-2">
                    <TabsTrigger
                        value="members"
                        className="flex items-center gap-2"
                    >
                        <UsersIcon className="h-4 w-4" />
                        {t("band.members")}
                    </TabsTrigger>
                    <TabsTrigger
                        value="invitations"
                        className="flex items-center gap-2"
                    >
                        <MailIcon className="h-4 w-4" />
                        {t("band.invitations")}
                    </TabsTrigger>
                </TabsList>

                <TabsContent value="members" className="space-y-4">
                    <div className="flex justify-between items-center">
                        <h3 className="text-lg font-semibold">{t("band.bandMembers")}</h3>
                        {isOwner && <InviteMember
                            bandId={band.id}
                            onSuccess={() => {
                                // TODO: Refresh member list
                            }}
                        />}
                    </div>
                    <MemberList bandId={band.id} />
                </TabsContent>

                <TabsContent value="invitations" className="space-y-4">
                    <InvitationList bandId={band.id} />
                </TabsContent>
            </Tabs>
        </div>
    );
}
