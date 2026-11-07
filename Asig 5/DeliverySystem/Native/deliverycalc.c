#if defined(_WIN32)
#define API __declspec(dllexport)
#else
#define API __attribute__((visibility("default")))
#endif
API int delivery_surcharge_cents(int distance_meters,int rate_cents_per_km) {
 if(distance_meters<0 || rate_cents_per_km<0) return -1;
 return (distance_meters*rate_cents_per_km)/1000;
}
