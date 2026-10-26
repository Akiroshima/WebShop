using MurketApi.Application.Dtos;
using MurketApi.Domain.Catalog;
using MurketApi.Domain.Common;

namespace MurketApi.Tests;

/// <summary>Личный кабинет и модуль менеджера: конфликты, лимиты, CRUD.</summary>
public sealed class ProfileAndAdminTests
{
    [Fact]
    public void Профиль_ОбновлениеИКонфликтыУникальности()
    {
        var host = new TestHost();

        var updated = host.Profile.UpdateProfile(TestHost.CustomerId,
            new UpdateProfileDto("Анна Кошкина", "anna.k@example.com", null));
        Assert.Equal("Анна Кошкина", updated.FullName);

        // Email занят админом.
        var ex = Assert.Throws<ConflictException>(() => host.Profile.UpdateProfile(TestHost.CustomerId,
            new UpdateProfileDto("Анна", "admin@murket.ru", null)));
        Assert.Equal("Пользователь с таким email уже существует", ex.Message);
    }

    [Fact]
    public void Профиль_НекорректныйEmailИТелефон_400ПоПолям()
    {
        var host = new TestHost();

        var ex = Assert.Throws<ValidationException>(() => host.Profile.UpdateProfile(TestHost.CustomerId,
            new UpdateProfileDto("", "не-email", "+7123")));
        Assert.Contains("fullName", ex.Errors.Keys);
        Assert.Contains("email", ex.Errors.Keys);
        Assert.Contains("phone", ex.Errors.Keys);
    }

    [Fact]
    public void СменаПароля_НеверныйТекущий_400()
    {
        var host = new TestHost();

        var ex = Assert.Throws<ValidationException>(() => host.Profile.ChangePassword(
            TestHost.CustomerId, new ChangePasswordDto("wrong!", "НовыйПароль7!"), null));
        Assert.Equal("Неверный текущий пароль", ex.Message);
    }

    [Fact]
    public void СменаПароля_УспехИОтзывПрочихСессий()
    {
        var host = new TestHost();
        var session1 = host.Auth.Login("customer", "customer123", "ip1").RefreshToken;
        var session2 = host.Auth.Login("customer", "customer123", "ip2").RefreshToken;

        // Меняем пароль из сессии 1: её refresh передаём как текущий.
        host.Profile.ChangePassword(TestHost.CustomerId,
            new ChangePasswordDto("customer123", "НовыйПароль7!"), session1);

        // Сессия 2 отозвана, сессия 1 жива.
        Assert.Throws<UnauthorizedException>(() => host.Auth.Refresh(session2));
        Assert.NotNull(host.Auth.Refresh(session1));
    }

    [Fact]
    public void Адреса_СозданиеОграничениеИЕдинственныйПоУмолчанию()
    {
        var host = new TestHost();

        var created = host.Profile.CreateAddress(TestHost.CustomerId, new AddressDraftDto(
            "Дача", "Анна Смирнова", "+79991234567", "Звенигород", "ул. Лесная", "1",
            null, null, null, null, null, IsDefault: true));
        Assert.True(created.IsDefault);

        // Прежний адрес по умолчанию сброшен.
        Assert.DoesNotContain(host.Profile.Addresses(TestHost.CustomerId),
            address => address.Id == "a-home" && address.IsDefault);
    }

    [Fact]
    public void Адреса_Лимит10_409()
    {
        var host = new TestHost(seedDemoData: false);

        for (var i = 0; i < 10; i++)
        {
            host.Profile.CreateAddress(TestHost.CustomerId, new AddressDraftDto(
                null, "Анна Смирнова", "+79991234567", "Москва", "ул. Новая", $"{i}",
                null, null, null, null, null, IsDefault: i == 0));
        }

        Assert.Throws<ConflictException>(() => host.Profile.CreateAddress(TestHost.CustomerId, new AddressDraftDto(
            null, "Анна Смирнова", "+79991234567", "Москва", "ул. Лишняя", "99",
            null, null, null, null, null, false)));
    }

    [Fact]
    public void Адреса_ЧужойИОтсутствующий_404()
    {
        var host = new TestHost();

        Assert.Throws<NotFoundException>(
            () => host.Profile.UpdateAddress(TestHost.AdminId, "a-home", new AddressDraftDto(
                null, "Админ", "+79991234567", "Москва", "ул. Лесная", "7",
                null, null, null, null, null, false)));
    }

    [Fact]
    public void Товары_ДубликатАртикула_409_СозданиеИУдаление()
    {
        var host = new TestHost();

        var draft = new ProductDraftDto("RC-777", "Новый корм", "Описание", "c-food", "b-triol",
            100m, 5, "assets/img/products/korm.svg", true);
        var created = host.Admin.CreateProduct(draft);
        Assert.Equal("RC-777", created.Sku);

        Assert.Throws<ConflictException>(() => host.Admin.CreateProduct(draft));
        Assert.Throws<ConflictException>(() => host.Admin.UpdateProduct(
            "p-rc-sterilised", new ProductDraftDto("RC-777", "Конфликт", "", "c-food", null,
                1m, 1, "img", true)));

        host.Admin.DeleteProduct(created.Id);
        Assert.Throws<NotFoundException>(() => host.Admin.UpdateProduct(created.Id, draft));
    }

    [Fact]
    public void Товары_НекорректнаяКатегория_400()
    {
        var host = new TestHost();

        var ex = Assert.Throws<ValidationException>(() => host.Admin.CreateProduct(new ProductDraftDto(
            "XX-001", "Товар", "", "c-нет", null, 10m, 1, "img", true)));
        Assert.Contains("categoryId", ex.Errors.Keys);
    }

    [Fact]
    public void Товары_ПоискИПагинация_ВключаяНеактивные()
    {
        var host = new TestHost();

        var page1 = host.Admin.Products(null, new PageRequest(1, 10));
        Assert.Equal(41, page1.Total); // все товары, включая 2 неактивных
        Assert.Equal(10, page1.Items.Count);

        var found = host.Admin.Products("rc-00", new PageRequest(1, 48));
        Assert.All(found.Items, product => product.Sku.StartsWith("RC-00"));
        Assert.Equal(5, found.Total); // RC-001..RC-005
    }

    [Fact]
    public void Пользователи_СписокТолькоЧтение()
    {
        var host = new TestHost();

        var users = host.Admin.Users();
        Assert.Equal(2, users.Count);
        Assert.Contains(users, user => user.Login == "admin" && user.Role == "admin");
    }

    [Fact]
    public void Каталог_ТолькоАктивные_ФильтрыСортировкаПагинация()
    {
        var host = new TestHost();

        // Активных 39 из 41.
        Assert.Equal(39, host.Catalog.Products(new CatalogQuery { Page = new PageRequest(1, 48) }).Total);

        // Категория «Игрушки» — 7 активных.
        var toys = host.Catalog.Products(new CatalogQuery
        {
            CategoryId = "c-toys",
            Page = new PageRequest(1, 48),
        });
        Assert.Equal(7, toys.Total);

        // Цена 500–1000: RC-004 нет (99), EC-001 890, TR-002 420 нет… считаем по данным.
        var priced = host.Catalog.Products(new CatalogQuery
        {
            PriceMin = 500,
            PriceMax = 1000,
            Page = new PageRequest(1, 48),
        });
        Assert.All(priced.Items, product => Assert.True(product.Price >= 500 && product.Price <= 1000));

        // Сортировка по цене возрастает.
        var sorted = host.Catalog.Products(new CatalogQuery
        {
            Sort = CatalogSort.PriceAsc,
            Page = new PageRequest(1, 48),
        });
        Assert.Equal(sorted.Items.Select(product => product.Price).OrderBy(price => price),
            sorted.Items.Select(product => product.Price));

        // Пагинация: страница 2 по 12.
        var page2 = host.Catalog.Products(new CatalogQuery { Page = new PageRequest(2, 12) });
        Assert.Equal(12, page2.Items.Count);
        Assert.Equal(39, page2.Total);
    }

    [Fact]
    public void Каталог_КатегорииИБрендыСКоличествами()
    {
        var host = new TestHost();

        var categories = host.Catalog.Categories();
        Assert.Equal(5, categories.Count);
        Assert.Equal(12, categories.Single(category => category.Id == "c-food").Count);

        // Бренды в категории «Игрушки» — только Triol.
        var brands = host.Catalog.Brands("c-toys");
        var brand = Assert.Single(brands);
        Assert.Equal(("b-triol", 7), (brand.Id, brand.Count));
    }

    [Fact]
    public void Каталог_КарточкаНеактивного_404()
    {
        var host = new TestHost();

        Assert.Throws<NotFoundException>(() => host.Catalog.Product("p-rc-light"));
        Assert.NotNull(host.Catalog.Product("p-rc-sterilised"));
    }

    [Fact]
    public void Каталог_ПоискПоНазваниюАртикулуБренду()
    {
        var host = new TestHost();

        var bySku = host.Catalog.Products(new CatalogQuery { Search = "rc-001", Page = new PageRequest(1, 48) });
        Assert.Single(bySku.Items);

        var byBrand = host.Catalog.Products(new CatalogQuery { Search = "grandorf", Page = new PageRequest(1, 48) });
        Assert.Equal(3, byBrand.Total);
    }
}
