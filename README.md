# ht-ent-config-seminar

# Common Extensions For Production
**WARNING!** This is a DEMO project only and should NOT be used for production scenarios.  If you
you this code consider at least the following:
1. Change and API request/response to align with your patterns
2. Enable security (OpenId/OAuth or Windows)
3. Force SSL
4. Add a swagger end-point
5. **Change the encryption key**
6. Consider how encryption will be managed.  Extend for rolling encryption keys
7. Limit database access to only identity of calling API
8. Do you need a UI for your team or is API sufficient?
9. Do you need logging/metrics