# object_recognition_enhance_AR
Enhancing AR experiences by integrating object recognition to allow for recognizing several objects that can be used to trigger various events in a single application.

There is a set limit to how man objects (~ 2 in most SDKs ) can be recognized in a single application, each requiring pre-scans of the objects. Understandable, since they are not only trying to recognize the objects but also using them as markers.

However, this is limiting if the sole purpose of recognizing objects is to trigger an event. My approach enhances AR experiences using object recognition models which allow AR applications to classify as many objects as our models are trained to detect. 

# Future Improvements
Got some funny but understandable results when importing 3D assets from a library at runtime, like getting back a Nintendo Switch instead of a light switch. Something that could be improved with more refined searches.
If we make use of object detection and detect multiple objects in a frame with respect to others, we will be able to spawn certain 3D objects at more precise locations.
