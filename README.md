# OneOfUnity [![Static Badge](https://img.shields.io/badge/license-MIT-98c610)](LICENSE.md)

## I am not the author of the underlying logic

This package is an adaptation of the amazing [OneOf](https://github.com/mcintyre321/OneOf/tree/master) library. If you find this package useful please check out the original and give it a star!

## Getting started

### Installation

- Go to `Window/Package Manager`
- Click on the plus [+] symbol in the top left corner of the window
- Click "Add package from git URL..."
- Paste in `https://github.com/Simply-Cods/OneOfUnity.git?path=/OneOfUnity#latest` (you can specify your desired version by replacing 'latest')

If you encounter any issues throughout the installation process, please refer to the official documentation from Unity [here](https://docs.unity3d.com/Manual/upm-ui-giturl.html)

## Use cases

### As a method return value

The most frequent use case is as a return value, when you need to return different results from a method. Here's how you might use it in a function that creates a user in a multiplayer lobby:
```csharp
public OneOf<User, InvalidName, NameTaken> CreateUser(string username)
{
  if (!IsValid(username)) return new InvalidName();
  var user = _playerList.FindByUsername(username);
  if (user != null) return new NameTaken();
  user = new User(username);
  _playerList.Add(user);
  return user;
}

public void OnSetNameButtonClick()
{
  OneOf<User, InvalidName, NameTaken> result = CreateUser(_usernameField.text);
  result.Switch(
    user => user.JoinGame(),
    invalidName => TriggerPopup("Sorry, this username is invalid"),
    nameTaken => TriggerPopup("Sorry, this username is already taken")
  );
}
```

### As an 'Option' Type

Using OneOf as an `Option` type is as easy as declaring a `OneOf<Something, None>`. OneOf comes with a variety of useful types in the `OneOf.Types` namespace, including `Yes` `No` `Maybe`, `Unknown`, `True`, `False`, `All`, `Some`, and `None`.

### As a method parameter value

You can also use `OneOf` as a parameter type, allowing a caller to pass different types without requiring additional overloads. this might not seem that useful for a single parameter, but if you g=have multiple parameters, the number of overloads required increases rapidly.

```csharp
public void SetBackground(OneOf<string, Color, ColorName>) { ... }

// The method above can be called with either a string, a Color instance or a ColorName enum value.
```

## Matching

You can use `TOut Match(Func<T0, TOut> f0, ... Func<Tn, TOut> fn)` method to get a value out. note how the number of handlers matches the number of generic arguments.

### Advantages over `switch` or `if` or `exception` based control flow:
- Requires every parameter to be handled
- No fallback - if you add another generic parameter, you HAVE to update all the calling code to handle your changes.

E.g.
```csharp
OneOf<string, ColorName, Color> backgroundColor = ...;
Color c = backgroundColor.Match(
    str =>
    {
      if (ColorUtility.TryParseHtmlString(str, out Color color))
        return color;
      throw new ArgumentException("Background color is not a valid html string", nameof(backgroundColor));
    },
    name => GetColorFromName(name),
    col => col
);
```

There is also a `.Switch` method, for when you aren't returning a value:
```csharp
OneOf<string, DateTime> dateValue = ...;
dateValue.Switch(
    str => AddEntry(DateTime.Parse(str), foo),
    int => AddEntry(int, foo)
);
```

### TryPick𝑥 method

As an alternative to `.Switch` or `.Match` you can use the `.TryPick𝑥` methods.

```csharp
//TryPick𝑥 methods for OneOf<T0, T1, T2>
public bool TryPickT0(out T0 value, out OneOf<T1, T2> remainder) { ... }
public bool TryPickT1(out T1 value, out OneOf<T0, T2> remainder) { ... }
public bool TryPickT2(out T2 value, out OneOf<T0, T1> remainder) { ... }
```

The return value indicates if the OneOf contains a T𝑥 or not. If so, then `value` will be set to the inner value from the OneOf. If not, then the remainder will be a OneOf of the remaining generic types. You can use them like this:

```csharp
public void OnSetNameButtonClick()
{
  OneOf<User, InvalidName, NameTaken> userOrInvalidNameOrNameTaken = CreateUser(_usernameField.text);
  if (userOrInvalidNameOrNameTaken.TryPickT1(out InvalidName invalidName, out var userOrNameTaken)) //userOrNameTaken is a OneOf<User, NameTaken>
  {
    TriggerPopup("Sorry, this username is invalid")
    _usernameField.text = string.Empty;
    return;
  }

  if(userOrNameTaken.TryPickT1(out NameTaken nameTaken, out User user)) //Note that user is of type User and not OneOf<User>
  {
    TriggerPopup("Sorry, this username is already taken")
    _usernameField.text = string.Empty;
    return;
  }

  user.JoinGame();
}
```

## Reusable OneOf Types using OneOfBase

You can declare a `OneOf` as a type, either for reuse of the type, or to provide additional members, by inheriting from `OneOfBase`. The derived class will inherit the `.Match`, `.Switch`, and `.TryPick𝑥` methods.

```csharp
public class StringOrNumber : OneOfBase<string, int>
{
  StringOrNumber(OneOf<string, int> _) : base(_) { }

  // optionally, define implicit conversions
  // you could also make the constructor public
  public static implicit operator StringOrNumber(string _) => new StringOrNumber(_);
  public static implicit operator StringOrNumber(int _) => new StringOrNumber(_);

  public (bool isNumber, int number) TryGetNumber() =>
    Match(
        s => (int.TryParse(s, out var n), n),
        i => (true, i)
    );
}

StringOrNumber x = 6;
Debug.Log(x.TryGetNumber().number);
// print 6

x = "9"
Debug.Log(x.TryGetNumber().number);
// print 9

x = "qwerty";
Debug.Log(x.TryGetNumber().isNumber);
// print False
```
