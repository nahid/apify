class Assert
{
    equals(expected, actual, message = null) {
        message = message ?? "Values are equal."
        
        if (expected !== actual) {
            return assertionTracker.AddResult(false, `Expected: ${expected}, Actual: ${actual} > ${message}`);
        }
    
        return assertionTracker.AddResult(true, message);
    }
    
    notEquals(expected, actual, message = null) {
        message = message ?? "Values are not equal."
        
        if (expected === actual) {
            return assertionTracker.AddResult(false, `Expected: ${expected}, Actual: ${actual} > ${message}`);
        }
    
        return assertionTracker.AddResult(true, message);
    }
    
    isTrue(value, message = null) {
        message = message ?? "Value is true."
        
        if (value !== true) {
            return assertionTracker.AddResult(false, `Expected: true, Actual: ${value} > ${message}`);
        }
    
        return assertionTracker.AddResult(true, message);
    }
    
    isFalse(value, message = null) {
        message = message ?? "Value is false."
        
        if (value !== false) {
            return assertionTracker.AddResult(false, `Expected: false, Actual: ${value} > ${message}`);
        }
    
        return assertionTracker.AddResult(true, message);
    }
    
    isNull(value, message = null) {
        message = message ?? "Value is null."
        
        if (value !== null) {
            return assertionTracker.AddResult(false, `Expected: null, Actual: ${value} > ${message}`);
        }
    
        return assertionTracker.AddResult(true, message);
    }
    
    isNotNull(value, message = null) {
        message = message ?? "Value is not null."
        
        if (value === null) {
            return assertionTracker.AddResult(false, `Expected: not null, Actual: ${value} > ${message}`);
        }
    
        return assertionTracker.AddResult(true, message);
    }
    
    isEmpty(value, message = null) {
        message = message ?? "Value is empty."
        
        if (value !== '' && value !== null && value !== undefined && value.length !== 0) {
            return assertionTracker.AddResult(false, `Expected: empty, Actual: ${value} > ${message}`);
        }
    
        return assertionTracker.AddResult(true, message);
    }
    
    isNotEmpty(value, message = null) {
        message = message ?? "Value is not empty."
        
        if (value === '' || value === null || value === undefined || value.length === 0) {
            return assertionTracker.AddResult(false, `Expected: not empty, Actual: ${value} > ${message}`);
        }
    
        return assertionTracker.AddResult(true, message);
    }
    
    isArray(value, message = null) {
        message = message ?? "Value is an array."
        
        if (!Array.isArray(value)) {
            return assertionTracker.AddResult(false, `Expected: array, Actual: ${value} > ${message}`);
        }
    
        return assertionTracker.AddResult(true, message);
    }
    
    isObject(value, message = null) {
        message = message ?? "Value is an object."
        
        if (typeof value !== 'object' || value === null || Array.isArray(value)) {
            return assertionTracker.AddResult(false, `Expected: object, Actual: ${value} > ${message}`);
        }
    
        return assertionTracker.AddResult(true, message);
    }
    
    isString(value, message = null) {
        message = message ?? "Value is a string."
        if (typeof value !== 'string') {
            return assertionTracker.AddResult(false, `Expected: string, Actual: ${value} > ${message}`);
        }
        return assertionTracker.AddResult(true, message);
    }
    
    isNumber(value, message = null) {
        message = message ?? "Value is a number."
        if (typeof value !== 'number' || isNaN(value)) {
            return assertionTracker.AddResult(false, `Expected: number, Actual: ${value} > ${message}`);
        }
        return assertionTracker.AddResult(true, message);
    }
    
    isBoolean(value, message = null) {
        message = message ?? "Value is a boolean."
        if (typeof value !== 'boolean') {
            return assertionTracker.AddResult(false, `Expected: boolean, Actual: ${value} > ${message}`);
        }
        return assertionTracker.AddResult(true, message);
    }
    
    contains(substring, string, message = null) {
        message = message ?? "String contains substring."
        
        if (typeof string !== 'string' || typeof substring !== 'string') {
            return assertionTracker.AddResult(false, `Expected: string containing "${substring}", Actual: ${string} > ${message}`);
        }
        
        if (!string.includes(substring)) {
            return assertionTracker.AddResult(false, `Expected: string containing "${substring}", Actual: ${string} > ${message}`);
        }
    
        return assertionTracker.AddResult(true, message);
    }
    
    notContains(substring, string, message = null) {
        message = message ?? "String does not contain substring."
        
        if (typeof string !== 'string' || typeof substring !== 'string') {
            return assertionTracker.AddResult(false, `Expected: string not containing "${substring}", Actual: ${string} > ${message}`);
        }
        
        if (string.includes(substring)) {
            return assertionTracker.AddResult(false, `Expected: string not containing "${substring}", Actual: ${string} > ${message}`);
        }
    
        return assertionTracker.AddResult(true, message);
    }
    
    matchesRegex(pattern, string, message = null) {
        message = message ?? "String matches regex pattern."
        
        if (typeof string !== 'string' || !(pattern instanceof RegExp)) {
            return assertionTracker.AddResult(false, `Expected: string matching regex "${pattern}", Actual: ${string} > ${message}`);
        }
        
        if (!pattern.test(string)) {
            return assertionTracker.AddResult(false, `Expected: string matching regex "${pattern}", Actual: ${string} > ${message}`);
        }
    
        return assertionTracker.AddResult(true, message);
    }
    
    notMatchesRegex(pattern, string, message = null) {
        message = message ?? "String does not match regex pattern."
        
        if (typeof string !== 'string' || !(pattern instanceof RegExp)) {
            return assertionTracker.AddResult(false, `Expected: string not matching regex "${pattern}", Actual: ${string} > ${message}`);
        }
        
        if (pattern.test(string)) {
            return assertionTracker.AddResult(false, `Expected: string not matching regex "${pattern}", Actual: ${string} > ${message}`);
        }
    
        return assertionTracker.AddResult(true, message);
    }
    
    hasProperty(object, property, message = null) {
        message = message ?? "Object has property."
        
        if (typeof object !== 'object' || object === null || !Object.prototype.hasOwnProperty.call(object, property)) {
            return assertionTracker.AddResult(false, `Expected: object with property "${property}", Actual: ${object} > ${message}`);
        }
    
        return assertionTracker.AddResult(true, message);
    }
    
    hasNotProperty(object, property, message = null) {
        message = message ?? "Object does not have property."
        
        if (typeof object !== 'object' || object === null || Object.prototype.hasOwnProperty.call(object, property)) {
            return assertionTracker.AddResult(false, `Expected: object without property "${property}", Actual: ${object} > ${message}`);
        }
    
        return assertionTracker.AddResult(true, message);
    }
    
    isGreaterThan(expected, actual, message = null) {
        message = message ?? "Value is greater than expected."
        
        if (actual <= expected) {
            return assertionTracker.AddResult(false, `Expected: greater than ${expected}, Actual: ${actual} > ${message}`);
        }
    
        return assertionTracker.AddResult(true, message);
    }
    
    isLessThan(expected, actual, message = null) {
        message = message ?? "Value is less than expected."
        
        if (actual >= expected) {
            return assertionTracker.AddResult(false, `Expected: less than ${expected}, Actual: ${actual} > ${message}`);
        }
    
        return assertionTracker.AddResult(true, message);
    }
    
}


