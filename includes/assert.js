const Assert = {
    equals: function (a, b) {
        if (a !== b) {
            throw new Error("Assertion failed: " + a + " is not equal to " + b);
        }
        
        return true;
    }
};
