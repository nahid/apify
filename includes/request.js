class Request
{
    #payload;
    
    constructor(payload)
    {
        this.#payload = JSON.parse(payload);
    }

    getName()
    {
        return this.#payload.name || '';
    }
    
    getHeaders()
    {
        return this.#payload.headers || {};
    }
    
    getBody()
    {
        return this.#payload.body || {};
    }
    
    getUrl()
    {
        return this.#payload.url || '';
    }
    
    getMethod()
    {
        return this.#payload.method || 'GET';
    }
    
    
}