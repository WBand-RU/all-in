import { UserIcon } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "../../../shared/ui/card";
import type { Band } from "../../../lib/generated-api/band-api/models";
import { useTransition } from "react";
import { useTranslation } from "react-i18next";
import { EditableText } from "../../../shared/ui/editable-text";
import { useUpdateBand } from "../../../lib/generated-api/band-api/bands";

interface Props {
    band: Band;
    isOwner: boolean;
}

export function BandInformationCard({ band, isOwner }: Props) {
    const { t } = useTranslation("translation");
    const { 
        mutateAsync: updateBand,
        isPending: isPendingUpdateBand, 
        isError: isErrorUpdateBand 
    } = useUpdateBand();

    async function handleNameEdited(newName: string) {
        try {
            await updateBand({
                bandId: band.id,
                data: {
                    name: newName,
                }
            });
        }
        catch (error) {
            console.error(error);
        }
    }

    return (
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
                    <span>
                        {isOwner ?
                            <EditableText
                                defaultValue={band.name}
                                onEdited={handleNameEdited} />
                            : band.name
                        }
                    </span>
                </div>
                <div className="flex justify-between">
                    <span className="text-sm font-medium text-muted-foreground">
                        ID
                    </span>
                    <span className="font-mono text-sm">{band.id}</span>
                </div>
            </CardContent>
        </Card>
    );
}