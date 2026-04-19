import http from 'k6/http';
import { sleep } from 'k6';

export const options = {
    vus: 10,
    duration: '30s',
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5285';

export default function () {
    http.get(`${BASE_URL}/api/chat/get-all-rooms`);
    sleep(1);
}