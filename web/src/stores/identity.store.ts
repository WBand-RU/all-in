import { create } from "zustand";

type Store = {
	accessToken?: string;
	setAccessToken: (token?: string) => void;
};

// TODO: just without persistence, if will need add persist()
export const useIdentityStore = create<Store>((set) => ({
	setAccessToken: (accessToken) => {
		set(x => ({
			...x,
			accessToken,
		}));
	},
}));