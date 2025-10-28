import AuthorizeView from "../Components/AuthorizeView";
import LogoutLink from "../Components/LogoutLink";
import { AuthorizedUser } from "../Components/AuthorizeView";
import Chat from "../Components/Chat";
import { useEffect } from "react";

function Home(){
    return(
        <AuthorizeView>
            <Chat>
                <span><LogoutLink>Logout <AuthorizedUser value='email'/></LogoutLink></span>
            </Chat>
        </AuthorizeView>
    )
}

export default Home;