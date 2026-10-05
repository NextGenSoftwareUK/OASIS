"use client";

import SocialNetwork from "@/app/profile/components/SocialNetwork";
import { Button } from "@/components/ui/button";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { socialNetworksForConnection } from "@/lib/helpers/socialNetworksForConnection";
import {
  personalInformationSchema,
  PersonalInformationSchema,
  personalInformationSchemaDefaultValues,
} from "@/schemas/profile/personalInformation.schema";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";

export default function PersonalnformationForm() {
  const form = useForm<PersonalInformationSchema>({
    resolver: zodResolver(personalInformationSchema),
    defaultValues: personalInformationSchemaDefaultValues,
  });

  const onSubmit = (data: PersonalInformationSchema) => console.log(data);

  return (
    <Form {...form}>
      <form className="pb-10" onSubmit={form.handleSubmit(onSubmit)}>
        <h2 className="h1 mb-7.5">Personal Infromation</h2>

        <div className="space-y-3.5">
          <FormField
            control={form.control}
            name="fullName"
            render={({ field }) => (
              <FormItem className="w-full">
                <FormLabel>Full Name</FormLabel>
                <FormControl>
                  <Input type="text" placeholder="John Doe" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="userName"
            render={({ field }) => (
              <FormItem className="w-full">
                <FormLabel>Username</FormLabel>
                <FormControl>
                  <Input type="text" placeholder="john_doe" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="email"
            render={({ field }) => (
              <FormItem className="w-full">
                <FormLabel>Email</FormLabel>
                <FormControl>
                  <Input type="email" placeholder="john@gmail.com" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="aboutYou"
            render={({ field }) => (
              <FormItem className="w-full">
                <FormLabel>About You</FormLabel>
                <FormControl>
                  <Textarea placeholder="I work as a programmer" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <h2 className="h1 mt-12.5 mb-1">Social Links</h2>
        <p className="p-sm mb-7.5 text-secondary">
          Add your existing social links to build a stronger reputation
        </p>

        <FormField
          control={form.control}
          name="websiteUrl"
          render={({ field }) => (
            <FormItem className="w-full">
              <FormLabel>Website url</FormLabel>
              <FormControl>
                <Input
                  type="text"
                  placeholder="https://something.com"
                  {...field}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="flex gap-2 mt-5">
          {socialNetworksForConnection.map((item) => (
            <SocialNetwork
              key={item.id}
              iconPath={item.iconPath}
              socialNetwork={item.socialNetwork}
              profile="@johnDoe"
            />
          ))}
        </div>

        <Button type="submit" className="mt-12.5" variant="default" size="lg">
          Save
        </Button>
      </form>
    </Form>
  );
}
