import { useEffect, useState } from "react";

export default function HealthCheck() {
    const [status, setStatus] = useState('loading...');

    useEffect(() => {
        fetch('/api/health')
            .then((r) => r.json())
            .then((data) => setStatus(data.status))
            .catch(() => setStatus('backend unreachable'));
    }, []);

    return <p>Browser → /api/health: <b>{status}</b></p>;

}