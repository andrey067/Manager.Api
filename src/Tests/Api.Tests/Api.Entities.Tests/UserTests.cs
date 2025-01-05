using Api.Database.Entities;

namespace Api.Tests.Api.Entities.Tests;

public class UserTests
{
    [Fact]
    public void Create_ReturnsUser_WithValidParameters()
    {
        var user = User.Create("John", "Doe", "john.doe@example.com", "password123");
        
        Assert.NotNull(user);
        Assert.Equal("John", user.Name.FirstName);
        Assert.Equal("Doe", user.Name.LastName);
        Assert.Equal("john.doe@example.com", user.Email);
        Assert.Equal("password123", user.Password);
    }

    [Fact]
    public void Create_ThrowsException_WhenFirstNameIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => User.Create("", "Doe", "john.doe@example.com", "password123"));
    }

    [Fact]
    public void Create_ThrowsException_WhenLastNameIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => User.Create("John", "", "john.doe@example.com", "password123"));
    }
    
    [Fact]
    public void Update_UpdatesUser_WithValidParameters()
    {
        var user = User.Create("John", "Doe", "john.doe@example.com", "password123");
        user.Update("Jane", "Smith", "jane.smith@example.com", "newpassword123");
        
        Assert.Equal("Jane", user.Name.FirstName);
        Assert.Equal("Smith", user.Name.LastName);
        Assert.Equal("jane.smith@example.com", user.Email);
        Assert.Equal("newpassword123", user.Password);
    }

    [Fact]
    public void Update_ThrowsException_WhenFirstNameIsEmpty()
    {
        var user = User.Create("John", "Doe", "john.doe@example.com", "password123");
        Assert.Throws<ArgumentException>(() => user.Update("", "Smith", "jane.smith@example.com", "newpassword123"));
    }

    [Fact]
    public void Update_ThrowsException_WhenLastNameIsEmpty()
    {
        var user = User.Create("John", "Doe", "john.doe@example.com", "password123");
        Assert.Throws<ArgumentException>(() => user.Update("Jane", "", "jane.smith@example.com", "newpassword123"));
    }
}