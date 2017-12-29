#
# Author: Bailey Belvis (https://github.com/philosowaffle)
#
A_OK_HTTP_CODES = [
    200,
    207
]

A_ERROR_HTTP_CODES = {
    400: "Request was invalid",
    401: "Invalid API key",
    403: "Bad OAuth scope",
    404: "Selector did not match any lights",
    422: "Missing or malformed parameters",
    426: "HTTP is required to perform transaction",
    # see http://api.developer.lifx.com/v1/docs/rate-limits
    429: "Rate limit exceeded",
    500: "API currently unavailable",
    502: "API currently unavailable",
    503: "API currently unavailable",
    523: "API currently unavailable"
}