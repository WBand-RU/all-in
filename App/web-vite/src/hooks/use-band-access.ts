import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/axios-instance";

export interface BandAccess {
    bandId: string;
    role: "Owner" | "Admin" | "Member";
    canEditContent: boolean;
}

export function useBandAccess() {
    const query = useQuery({
        queryKey: ["my-band-access"],
        queryFn: async () => (await apiClient.get<{ value: BandAccess[] }>(
            "/band-api/user/band-access",
        )).data.value ?? [],
    });

    return {
        ...query,
        access: query.data ?? [],
        canEditBand: (bandId: string) =>
            query.data?.some(item => item.bandId === bandId && item.canEditContent) ?? false,
    };
}
