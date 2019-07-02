#!/usr/bin/env python
'''
This file includes examples of how to use the API.  You will have to obtain your own API Key to use the API.
The API Key must be put in the request header of all requests to the API.  There are two main ways to use 
the API-- the first is to retrieve proxies, and teh second is to report their success.  Please be sure to 
try and follow through by reporting successes and failures of proxies you retrieve.  Proxy data in the under-
lying database starts with a very low success rate, and then the success rate of proxies increases as users
vet proxies and report their results-- reported successes will give proxies a higher score, and reported
failures will lower the score, until the system decides that it's safe to say the proxy is not going to work
and disables it.

IMPORTANT-- You will see that you have the option to specify the site you are using, or to omit that information.
When reporting successes and failures, if you specify the website, the failure will only be registered for that
site alone.  So a site that is reported to have failed 10 times for lowes.com may still be considered valid for
bestbuy.com, since you really aren't sure that it doesn't work for bestbuy.com until you try it.  If the proxy
has found to fail for many sites repetitively, the system will eventually realize taht it's just not working for
any site at all and disable it.  However, if you omit the website when reporting successes/failures, this counts
against the proxy itself (for all websites), which has a much more far-reaching impact.  So keep this in mind
when deciding whether or not to report the website when reporting successes/failures.  It will ultimately come
down to whether or not you are confident that the failure was a result of the proxy in general or something
specific about the website.

Lastly note that this is running on a gen2 core i5 PC with a spinning drive sitting in my basement.  It's used for
other things too, and it often restarts itself and is offline for short periods of time-- also performance is probably 
going to underwhelm a little.  I would recommend taking the approach of using the API to retrieve batches of proxies
and store them locally for use instead of relying on this API to be online and performant at the moment you
need to use the proxy.  I also may have to throttle proxy requests per hour to reduce the amount of database
queries-- 1 request for 100 proxies would be preferable to 100 requests for 1 proxy each, but I wouldn't bend
over backwards to accomplish it this way if it's an issue for you.
'''
import requests
import json

# How to configure session to use API Key
headers = {
	'APIKey': 'c9a12ad3-025c-4cf6-a454-f163e75dc205' # Use your API key.  This one will be invalid.
}
session = requests.session()
session.headers = headers

# How to use the API to retrieve 2 proxies for lowes.com
response = session.get("http://url.here/api/proxy/list/lowes.com/2")
# Note that the quantity provided may fall short of what was requested by up to 50%,
# but should usually match or be within 10%
if (response.status_code == 403):
	print("Authentication Error.  API Key was probably wrong or missing.")
elif (response.status_code == 200):
	for proxy in json.loads(response.text):
		url = proxy["url"]
		print(url)
		'''
		Unvetted Proxy Example:
		{
		'proxyID': 1031247,                       -- The ID used for this proxy in the API
		'url': 'http://91.150.67.210:55318',      -- Proxy Location
		'proxyScore': 0,                          -- Proxy Score (higher is better).\  
											      -- "0" probably means that this proxy has not been tested yet.
		'country': 'Serbia',                      -- Country of Proxy (not always correct in practice)
		'streak': 0,                              -- How many consecutive successes has this proxy had?
		'site': None,                             -- The site that this proxy's site score is for.\
												     "None" means the proxy hasn't been tested against the requested site yet.
		'siteScore': None,                        -- The proxy's score for the given site (higher is better).\
												     "None" means the proxy hasn't been tested against the requested site yet.
		'source': 'my list',                      -- The site, API, etc. that provided the API with the proxy's location
		'siteVetted': False                       -- True if this proxy has been tested against the given site, false if it hasn't been tested yet.
		}
		
		Vetted Proxy Example:
		'proxyID': 98797, 
		'url': 'http://186.47.62.94:44892', 
		'proxyScore': 3,      -- score of 3 for proxy as a whole
		'country': 'Ecuador', 
		'streak': -1, 
		'site': 'lowes.com', 
		'siteScore': 18,      -- site score of 18 for lowes.com
		'source': 'my list', 
		'siteVetted': True
		'''

# How to contribute to the database by indicating if a proxy is working or not.
session.headers = {
	'Content-type' : 'application/json',
	'APIKey': 'c9a12ad3-025c-4cf6-a454-f163e75dc205', # Use your API key.  This one will be invalid.
}

json_data = ("{'site' : 'lowes.com', "     # The site all of the successes/failures are being reported for.  You can
									  # omit this if you are not tracking it, though it is most useful to have.
	       	 "'successes' : 5, "           # Indicate that the proxy has succeeded 5 times.
			 "'failures' : 1, "             # Indicate that the proxy failed 1 time.
			 "'banned' : 'False'} " )        # Set this to true if you know the proxy is banned for the provided site.
		                              # An example of a ban is a case where your proxy is from New Zealand
									  # and the site loads a page indicating that the merchant only does business in
									  # North America.  Since this would definitively tell you that the proxy will never
									  # work with the given site, the banned indicator is a means of immediately cutting
									  # off this proxy from the given site, instead of relying on scoring attempts to 
									  # eventually decide that it isn't working.

response = session.put("http://url.here/api/proxy/131248", data=json_data)
if (response.status_code == 403):
	print("Authentication Error.  API Key was probably wrong or missing.")
elif (response.status_code == 400):
	print("400.  This usually means the JSON was not encoded properly.")
	print(response.text)
elif (response.status_code == 204):
	print("Success/Failure update was successful.")
else:
	print("Unknown Failure.")
	print(response.status_code)
	print(response.text)
	
