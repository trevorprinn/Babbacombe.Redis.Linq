# Babbacombe.Redis.Linq
Linq extensions for Redis List and Hash Keys.

**This library is WIP and has not been properly tested yet.**

## RedisListKey

Treats a List Key as an IList object. The non-generic version treats the values as strings. The generic version uses an object implementing `ISerializer<T>` to
serialise and deserialise the items within the list. By default, the ISerializer uses JsonConvert to treat the values as Json strings.

When treating the list as an array, negative indices are treated as counting back from the end of the array, as is normal with Redis.

## RedisHashKey

Treats a Hash Key as an IDictionary object. The non-generic version treats the keys and values as strings. The generic version with 2 types uses TK for
keys and TV for values, with a serializer for each. The generic version with 1 type treats the keys as strings.
By default, the ISerializers use JsonConvert to treat the keys and/or values as Json strings.

## Serializers

These are simple classes, implementing `ISeralizer<T>`, that have a method to convert a T to string (Serialize), and another to convert a string to
a T (Deserialize). If you are using only strings and objects serialized in Json there is no need to supply your own seralizers, or even pass any into
the constructors, as the supplied serializers will automatically be created and handle the serialization operations by default.
<hr/>

The library is available from the [Babbacombe myget (nuget) feed](https://www.myget.org/gallery/babbacom-feed)    
https://www.myget.org/F/babbacom-feed/api/v2