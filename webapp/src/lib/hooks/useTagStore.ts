import {Tag} from "@/lib/types";
import {create} from "zustand/react";

type TagStore = {
    tags: Tag[];
    setTags: (tags: Tag[]) => void;
    getTagsBySlug: (slug: string | undefined) => Tag | undefined;
}

export const useTagStore = create<TagStore>((set, get) => ({
    tags: [],
    setTags: (tags) => set({tags}),
    getTagsBySlug: (slug) => get().tags.find(tag => tag.slug === slug),
}));