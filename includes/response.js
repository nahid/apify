class Response {
    #response = '{\"isSuccessful\":true,\"statusCode\":201,\"headers\":{\"Date\":\"Mon, 28 Jul 2025 08:20:34 GMT\",\"Connection\":\"keep-alive\",\"CF-RAY\":\"9662f543a8563e62-SIN\",\"Access-Control-Allow-Origin\":\"*\",\"ETag\":\"W/\\"6d-cXuEz15EcsGlWN5+EhLqiF/cR7w\\"\",\"Nel\":\"{\\"report_to\\":\\"heroku-nel\\",\\"response_headers\\":[\\"Via\\"],\\"max_age\\":3600,\\"success_fraction\\":0.01,\\"failure_fraction\\":0.1}\",\"Ratelimit-Limit\":\"100\",\"Ratelimit-Policy\":\"100;w=60\",\"Ratelimit-Remaining\":\"98\",\"Ratelimit-Reset\":\"60\",\"Referrer-Policy\":\"strict-origin-when-cross-origin\",\"Report-To\":\"{\\"group\\":\\"heroku-nel\\",\\"endpoints\\":[{\\"url\\":\\"https://nel.heroku.com/reports?s=ZMaQi%2BDqQp7KvFxXL0SbD0XpdPUMH%2B%2B5XiXMbfeDGj4%3D\\u0026sid=c4c9725f-1ab0-44d8-820f-430df2718e11\\u0026ts=1753690834\\"}],\\"max_age\\":3600}\",\"Reporting-Endpoints\":\"heroku-nel=\\"https://nel.heroku.com/reports?s=ZMaQi%2BDqQp7KvFxXL0SbD0XpdPUMH%2B%2B5XiXMbfeDGj4%3D&sid=c4c9725f-1ab0-44d8-820f-430df2718e11&ts=1753690834\\"\",\"Via\":\"1.1 heroku-router\",\"X-Content-Type-Options\":\"nosniff\",\"X-Frame-Options\":\"DENY\",\"X-Request-ID\":\"0fc66773-dd6c-e68e-b0be-d59cdc83681f\",\"X-XSS-Protection\":\"1; mode=block\",\"cf-cache-status\":\"DYNAMIC\",\"Server\":\"cloudflare\",\"Server-Timing\":\"cfL4;desc=\\"?proto=TCP&rtt=64952&min_rtt=63338&rtt_var=14433&sent=6&recv=8&lost=0&retrans=0&sent_bytes=3128&recv_bytes=542&delivery_rate=65709&cwnd=254&unsent_bytes=0&cid=6631d2345b43409a&ts=664&x=0\\"\"},\"contentHeaders\":{\"Content-Type\":\"application/json; charset=utf-8\",\"Content-Length\":\"109\"},\"contentType\":\"application/json\",\"body\":\"{\\"name\\":\\"Cecil Cassin V\\",\\"job\\":\\"Legacy Identity Developer\\",\\"id\\":\\"463\\",\\"createdAt\\":\\"2025-07-28T08:20:34.815Z\\"}\",\"json\":{\"name\":\"Cecil Cassin V\",\"job\":\"Legacy Identity Developer\",\"id\":\"463\",\"createdAt\":\"2025-07-28T08:20:34.815Z\"},\"responseTimeMs\":1198,\"errorMessage\":null}'
    json = null;
    constructor(response) {
        this.#response = JSON.parse(response);
        if (this.#response.json) {
            this.json = this.#response.json;
        } else {
            try {
                this.json = JSON.parse(this.#response.body);
            } catch (e) {
                this.json = {};
            }
        }
    }
    
    isSuccessful() {
        return this.#response.isSuccessful || false;
    }
    
    getStatusCode() {
        return this.#response.statusCode || 200;
    }
    
    getHeaders() {
        return this.#response.headers || {};
    }
    
    getHeader(name) {
        return this.#response.headers[name] || '';
    }
    
    getContentHeaders() {
        return this.#response.contentHeaders || {};
    }
    
    getContentType() {
        return this.#response.contentType || '';
    }
    
    getBody() {
        return JSON.stringify(this.#response.body) || '';
    }
    
    getJson()
    {
        return this.json || {};
    }
    
}