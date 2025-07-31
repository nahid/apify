class Response {
    #response;
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