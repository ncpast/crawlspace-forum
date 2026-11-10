import { useEffect, useState } from "react";
import { getHealth } from "../lib/api";

export default function HealthCheck() {
    const [status, setStatus] = useState('loading...');

    useEffect(() => {
        getHealth()
            .then((data) => setStatus(data?.status ?? "unknown"))
            .catch(() => setStatus('backend unreachable'));
    }, []);

    return <p>Browser → /api/health: <b>{status}</b></p>;

}