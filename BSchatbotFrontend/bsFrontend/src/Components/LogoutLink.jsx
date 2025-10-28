
import { useNavigate } from "react-router-dom";

function LogoutLink(props) {

    const navigate = useNavigate();


    const handleSubmit = (e) => {
        e.preventDefault();
        fetch("https://localhost:7127/logout", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            credentials: 'include', // include cookies in the request
            body: JSON.stringify({})
        })
        .then((data) => {
            if (data.ok) {
                navigate("/login");
            }
            else { }
        })
        .catch((error) => {
            console.error(error);
        })

    };

    return (
        <>
            <a href="#" onClick={handleSubmit}>{props.children}</a>
        </>
    );
}

export default LogoutLink;