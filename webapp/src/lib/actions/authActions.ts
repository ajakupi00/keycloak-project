'use server'

import {fetchClient} from "@/lib/fetchClient";

export async function testAuth() {
    return fetchClient<string>(`/test/auth`, 'GET');
}