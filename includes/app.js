const apify = {
    
}

const $ = apify;


function tryParseJson(input) {
    try {
        return JSON.parse(input);
    } catch {
        return input;
    }
}