import NextAuth from "next-auth"
import Keycloak from "next-auth/providers/keycloak"
import {apiConfig, authConfig} from "@/lib/config";

export const { handlers, signIn, signOut, auth } = NextAuth({
    providers: [Keycloak({
        authorization: {
            // This line of code by Keycloack default options allows us to get the refresh token up to 30days
            // Something that Stripe uses for example, where you can get the token up to 7 days without logging in
            params: {scope: 'openid profile email offline_access'},
            url: `${authConfig.kcIssuer}/protocol/openid-connect/auth`
        },
        token: `${authConfig.kcInternal}/protocol/openid-connect/token`,
        userinfo: `${authConfig.kcInternal}/protocol/openid-connect/userinfo`,
    })],
    callbacks: {
        async jwt({token, account})
        {
            // Refresh token impl
            const now = Math.floor(Date.now() / 1000);
            
            

            // This statement is only true when we firstly login - no session
            if (account && account.access_token && account.refresh_token){
                const res = await fetch(apiConfig.baseUrl + "/profiles/me", {
                    headers: {
                        Authorization: `Bearer ${account.access_token}`,
                    }
                });
                
                if (res.ok)
                    token.user = await res.json();
                else 
                    console.log('Failed to fetch user profile', await res.text());
                
                
                token.accessToken = account.access_token;
                token.refreshToken = account.refresh_token;
                token.accessTokenExpires = now + account.expires_in!;
                token.error = undefined;
                return token;
            }
            
            // If token has not expired, return token
            if (token.accessTokenExpires && now < token.accessTokenExpires) 
                return token;
            
            // If token has expired, fetch new
            try {
                const response = await fetch(
                    `${authConfig.kcInternal}/protocol/openid-connect/token`,
                    {
                       method: 'POST',
                       headers: {'Content-Type': 'application/x-www-form-urlencoded'},
                       body: new URLSearchParams({
                           grant_type: 'refresh_token',
                           client_id: authConfig.kcClientId,
                           client_secret: authConfig.kcSecret,
                           refresh_token: token.refreshToken as string,
                       }) 
                    });
                
                const refreshed = await response.json();
                if (!response.ok)
                {
                    console.log("Failed to refresh token", refreshed);
                    token.error =  'RefreshAccessTokenError';
                    return token;
                }
                
                token.accessToken = refreshed.access_token;
                token.refreshToken = refreshed.refresh_token;
                token.accessTokenExpires = now + refreshed.expires_in!;
            } catch (error)
            {
                console.error('Failed to refresh token', error);
                token.error = 'RefreshAccessTokenError';
            }
            
            return token;
        },
        async session ({session, token}) 
        {
            if (token.user)
                session.user = token.user;
            if(token.accessToken)
                session.accessToken = token.accessToken;
            
            if (token.accessTokenExpires) 
                session.expires = new Date(token.accessTokenExpires * 1000) as unknown as typeof session.expires;
            
            return session;
        }
    }
});