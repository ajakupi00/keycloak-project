import ErrorButtons from "@/app/session/ErrorButtons";
import AuthTestButton from "@/app/session/AuthTestButton";

export default function Page() {
    return (
        <div className="flex items-center gap-3 justify-center mt-6">
            <ErrorButtons />
            <AuthTestButton />
        </div>
    );
}
