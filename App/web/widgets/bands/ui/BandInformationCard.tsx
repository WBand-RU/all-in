"use client";

import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { UserIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { getGetListOfBandsQueryKey } from "@/lib/generated/web-api/client";
import type { Band } from "@/lib/generated/web-api/models";
import { EditBand } from "./EditBand";

export function BandInformationCard({
    band,
    isOwner,
}: {
    band: Band;
    isOwner: boolean;
}) {
    const { t } = useTranslation();
    const [editing, setEditing] = useState(false);
    const queryClient = useQueryClient();
    return (
        <Card>
            <CardHeader>
                <CardTitle className="flex items-center gap-2">
                    <UserIcon className="h-5 w-5" />
                    {t("band.information")}
                </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
                <div className="flex items-center justify-between gap-2">
                    <span className="text-sm text-muted-foreground">
                        {t("common.name")}
                    </span>
                    {isOwner ? (
                        <Button
                            variant="ghost"
                            onClick={() => setEditing(true)}
                            aria-label={`${t("common.edit")}: ${band.name}`}
                        >
                            {band.name}
                        </Button>
                    ) : (
                        <span>{band.name}</span>
                    )}
                </div>
                <div className="flex justify-between gap-2">
                    <span className="text-sm text-muted-foreground">ID</span>
                    <span className="font-mono text-sm break-all">
                        {band.id}
                    </span>
                </div>
                {isOwner && band.id && (
                    <EditBand
                        open={editing}
                        bandId={band.id}
                        onClose={async ({ needForRefetch }) => {
                            if (needForRefetch)
                                await queryClient.invalidateQueries({
                                    queryKey: getGetListOfBandsQueryKey(),
                                });
                            setEditing(false);
                        }}
                    />
                )}
            </CardContent>
        </Card>
    );
}
